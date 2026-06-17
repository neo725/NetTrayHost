using System.Diagnostics;
using NetTrayHost.Models;

namespace NetTrayHost
{
    internal sealed class ProcessManager : IDisposable
    {
        private static readonly TimeSpan ConsoleHandleTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(1);

        private readonly ProcessConfigModel _config;
        private readonly AppLogger _logger;
        private readonly SynchronizationContext _syncContext;
        private readonly Action<ProcessManager>? _stateChanged;
        private readonly object _gate = new();

        private Process? _process;
        private IntPtr _consoleWindowHandle;
        private bool _userRequestedStop;
        private bool _disposed;
        private int _autoRestartAttempts;

        public ProcessManager(
            ProcessConfigModel config,
            AppLogger logger,
            SynchronizationContext syncContext,
            Action<ProcessManager>? stateChanged = null)
        {
            _config = config;
            _logger = logger;
            _syncContext = syncContext ?? throw new ArgumentNullException(nameof(syncContext));
            _stateChanged = stateChanged;
        }

        public string Name => _config.Name;
        public bool IsRunning => _process is { HasExited: false };
        public bool HasConsoleWindow => _consoleWindowHandle != IntPtr.Zero;
        public bool IsConsoleWindowVisible => HasConsoleWindow && NativeMethods.IsWindowVisible(_consoleWindowHandle);

        public bool TryAdopt()
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (IsRunning) return false;
            }

            var exePath = Path.GetFullPath(_config.Exe);
            Process? adopted = null;

            foreach (var candidate in Process.GetProcesses())
            {
                try
                {
                    if (!candidate.HasExited && string.Equals(
                            Path.GetFullPath(candidate.MainModule?.FileName ?? string.Empty),
                            exePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        adopted = candidate;
                        break;
                    }
                }
                catch { /* skip inaccessible processes */ }
                finally
                {
                    if (!ReferenceEquals(candidate, adopted))
                        candidate.Dispose();
                }
            }

            if (adopted is null) return false;

            lock (_gate)
            {
                ThrowIfDisposed();
                adopted.EnableRaisingEvents = true;
                adopted.Exited += OnProcessExited;
                _process = adopted;
                _userRequestedStop = false;
                _logger.Info($"Process '{Name}' adopted existing instance. PID={adopted.Id}.");
            }

            CaptureConsoleWindow();
            NotifyStateChanged();
            return true;
        }

        public void Start()
        {
            lock (_gate)
            {
                ThrowIfDisposed();

                if (IsRunning)
                {
                    _logger.Info($"Process '{Name}' start skipped because it is already running.");
                    return;
                }

                _userRequestedStop = false;
                _consoleWindowHandle = IntPtr.Zero;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _config.Exe,
                        WorkingDirectory = string.IsNullOrWhiteSpace(_config.WorkingDirectory)
                            ? Path.GetDirectoryName(_config.Exe) ?? AppContext.BaseDirectory
                            : _config.WorkingDirectory,
                        Arguments = _config.Arguments,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    },
                    EnableRaisingEvents = true
                };

                process.Exited += OnProcessExited;
                process.Start();
                _process = process;
                _logger.Info($"Process '{Name}' started. PID={process.Id}, startVisible={_config.StartVisible}.");
            }

            CaptureConsoleWindow();

            if (!_config.StartVisible)
            {
                HideConsoleWindow();
            }

            NotifyStateChanged();
        }

        public void Stop()
        {
            Process? process;
            lock (_gate)
            {
                ThrowIfDisposed();

                if (!IsRunning)
                {
                    _logger.Info($"Process '{Name}' stop skipped because it is not running.");
                    return;
                }

                _userRequestedStop = true;
                process = _process;
                _logger.Info($"Process '{Name}' stopping by user request. PID={process!.Id}.");
            }

            try
            {
                process!.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error($"Process '{Name}' stop failed.", ex);
            }
        }

        public bool ShowConsoleWindow()
        {
            if (!EnsureConsoleWindow())
            {
                _logger.Info($"Process '{Name}' show skipped because console window handle is unavailable.");
                return false;
            }

            var exStyle = NativeMethods.GetWindowLong(_consoleWindowHandle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_consoleWindowHandle, NativeMethods.GWL_EXSTYLE,
                (exStyle & ~NativeMethods.WS_EX_TOOLWINDOW) | NativeMethods.WS_EX_APPWINDOW);

            var result = NativeMethods.ShowWindow(_consoleWindowHandle, NativeMethods.SW_SHOW);
            _logger.Info($"Process '{Name}' console show requested. Result={result}.");
            NotifyStateChanged();
            return result;
        }

        public bool HideConsoleWindow()
        {
            if (!EnsureConsoleWindow())
            {
                _logger.Info($"Process '{Name}' hide skipped because console window handle is unavailable.");
                return false;
            }

            var exStyle = NativeMethods.GetWindowLong(_consoleWindowHandle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(_consoleWindowHandle, NativeMethods.GWL_EXSTYLE,
                (exStyle & ~NativeMethods.WS_EX_APPWINDOW) | NativeMethods.WS_EX_TOOLWINDOW);

            var result = NativeMethods.ShowWindow(_consoleWindowHandle, NativeMethods.SW_HIDE);
            _logger.Info($"Process '{Name}' console hide requested. Result={result}.");
            NotifyStateChanged();
            return result;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopForHostShutdown();

            Process? toDispose;
            lock (_gate)
            {
                toDispose = _process;
                _process = null;
            }
            toDispose?.Dispose();
        }

        public void StopForHostShutdown()
        {
            Process? process;
            lock (_gate)
            {
                if (!IsRunning)
                {
                    return;
                }

                _userRequestedStop = true;
                process = _process;
                _logger.Info($"Process '{Name}' stopping because NetTrayHost is shutting down. PID={process!.Id}.");
            }

            try
            {
                process!.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error($"Process '{Name}' shutdown stop failed.", ex);
            }
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            _syncContext.Post(_ => HandleProcessExited(), null);
        }

        private void HandleProcessExited()
        {
            bool shouldRestart;
            int restartAttempt;
            int? exitCode = null;

            lock (_gate)
            {
                if (_process is null)
                {
                    return;
                }

                try
                {
                    exitCode = _process.ExitCode;
                }
                catch (InvalidOperationException)
                {
                }

                _logger.Info($"Process '{Name}' exited. PID={_process.Id}, ExitCode={exitCode?.ToString() ?? "unknown"}, userRequestedStop={_userRequestedStop}.");

                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
                _consoleWindowHandle = IntPtr.Zero;

                shouldRestart = !_disposed
                    && !_userRequestedStop
                    && _config.AutoRestart
                    && _autoRestartAttempts < _config.MaxAutoRestartAttempts;

                if (shouldRestart)
                {
                    _autoRestartAttempts++;
                    restartAttempt = _autoRestartAttempts;
                    _logger.Info($"Process '{Name}' auto restart scheduled. Attempt={restartAttempt}/{_config.MaxAutoRestartAttempts}.");
                }
                else
                {
                    restartAttempt = _autoRestartAttempts;
                    if (!_userRequestedStop && _config.AutoRestart && _autoRestartAttempts >= _config.MaxAutoRestartAttempts)
                    {
                        _logger.Info($"Process '{Name}' auto restart stopped after reaching max attempts ({_config.MaxAutoRestartAttempts}).");
                    }
                    _autoRestartAttempts = 0;
                }

                _userRequestedStop = false;
            }

            NotifyStateChanged();

            if (shouldRestart)
            {
                Task.Delay(RestartDelay).ContinueWith(_ =>
                    _syncContext.Post(_ =>
                    {
                        try
                        {
                            Start();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Process '{Name}' auto restart attempt {restartAttempt}/{_config.MaxAutoRestartAttempts} failed.", ex);
                        }
                    }, null));
            }
        }

        private bool EnsureConsoleWindow()
        {
            if (_consoleWindowHandle != IntPtr.Zero)
            {
                return true;
            }

            return CaptureConsoleWindow();
        }

        private bool CaptureConsoleWindow()
        {
            Process? process;
            lock (_gate)
            {
                process = _process;
            }

            if (process is null || process.HasExited)
            {
                return false;
            }

            var deadline = DateTimeOffset.Now + ConsoleHandleTimeout;
            while (DateTimeOffset.Now < deadline)
            {
                var handle = GetConsoleWindowHandle(process.Id);
                if (handle != IntPtr.Zero)
                {
                    _consoleWindowHandle = handle;
                    _logger.Info($"Process '{Name}' console window handle captured. PID={process.Id}, HWND=0x{handle.ToInt64():X}.");
                    NotifyStateChanged();
                    return true;
                }

                if (process.HasExited)
                {
                    return false;
                }

                Thread.Sleep(100);
            }

            _logger.Info($"Process '{Name}' console window handle unavailable after {ConsoleHandleTimeout.TotalSeconds:0.#} seconds. PID={process.Id}.");
            return false;
        }

        private static IntPtr GetConsoleWindowHandle(int processId)
        {
            NativeMethods.FreeConsole();

            if (!NativeMethods.AttachConsole((uint)processId))
            {
                return IntPtr.Zero;
            }

            var handle = NativeMethods.GetConsoleWindow();
            NativeMethods.FreeConsole();
            return handle;
        }

        private void NotifyStateChanged()
        {
            _stateChanged?.Invoke(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProcessManager));
            }
        }
    }
}
