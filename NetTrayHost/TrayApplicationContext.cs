using NetTrayHost.Models;

namespace NetTrayHost
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private sealed class ProcessMenuState
        {
            public required ProcessConfigModel Config { get; init; }
            public required ProcessManager Manager { get; init; }
            public required ToolStripMenuItem RootMenuItem { get; init; }
            public required ToolStripMenuItem StartMenuItem { get; init; }
            public required ToolStripMenuItem StopMenuItem { get; init; }
            public required ToolStripMenuItem ShowMenuItem { get; init; }
            public required ToolStripMenuItem HideMenuItem { get; init; }
            public required ToolStripMenuItem AutoStartMenuItem { get; init; }
        }

        private readonly ConfigLoader _configLoader;
        private readonly AppConfigModel _config;
        private readonly AppLogger _logger;
        private readonly RegistryRunManager _registryRunManager;
        private readonly List<ProcessMenuState> _processMenuStates = [];
        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _netTrayHostStartupMenuItem;
        private readonly Image _runningImage;
        private readonly Image _stoppedImage;

        public TrayApplicationContext()
        {
            if (SynchronizationContext.Current is null)
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            }

            _logger = new AppLogger();
            _registryRunManager = new RegistryRunManager(_logger);
            _configLoader = new ConfigLoader();
            _config = _configLoader.Load();
            _runningImage = CreateStatusImage(Color.LimeGreen);
            _stoppedImage = CreateStatusImage(Color.Red);

            var menu = new ContextMenuStrip
            {
                ImageScalingSize = new Size(16, 16)
            };

            foreach (var processConfig in _config.Processes)
            {
                var processMenuState = CreateProcessMenuState(processConfig);
                _processMenuStates.Add(processMenuState);
                menu.Items.Add(processMenuState.RootMenuItem);
            }

            if (_processMenuStates.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
            }

            var settingsMenuItem = new ToolStripMenuItem("設定");
            _netTrayHostStartupMenuItem = new ToolStripMenuItem("NetTrayHost 開機自動啟動", null, (_, _) => ToggleNetTrayHostStartup())
            {
                CheckOnClick = false
            };
            settingsMenuItem.DropDownItems.Add(_netTrayHostStartupMenuItem);
            menu.Items.Add(settingsMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Opening += (_, _) => UpdateNetTrayHostStartupMenuState();

            menu.Items.Add("結束 NetTrayHost", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(typeof(TrayApplicationContext).Assembly.GetManifestResourceStream("NetTrayHost.app.ico")!, 16, 16),
                Text = "NetTrayHost",
                Visible = true,
                ContextMenuStrip = menu
            };

            foreach (var processMenuState in _processMenuStates)
            {
                UpdateProcessMenuState(processMenuState);
            }

            UpdateNetTrayHostStartupMenuState();
            StartAutoStartProcesses();
        }

        private ProcessMenuState CreateProcessMenuState(ProcessConfigModel config)
        {
            ProcessMenuState? processMenuState = null;
            var manager = new ProcessManager(config, _logger, SynchronizationContext.Current!, _ =>
            {
                if (processMenuState is not null)
                {
                    UpdateProcessMenuState(processMenuState);
                }
            });

            var rootMenuItem = new ToolStripMenuItem();
            var startMenuItem = new ToolStripMenuItem("啟動", null, (_, _) => manager.Start());
            var stopMenuItem = new ToolStripMenuItem("停止", null, (_, _) => manager.Stop());
            var showMenuItem = new ToolStripMenuItem("顯示視窗", null, (_, _) => manager.ShowConsoleWindow());
            var hideMenuItem = new ToolStripMenuItem("隱藏視窗", null, (_, _) => manager.HideConsoleWindow());
            var autoStartMenuItem = new ToolStripMenuItem("跟隨 NetTrayHost 自動啟動", null, (_, _) => ToggleAutoStart(config))
            {
                CheckOnClick = false
            };

            rootMenuItem.DropDownItems.AddRange([
                startMenuItem,
                stopMenuItem,
                showMenuItem,
                hideMenuItem,
                new ToolStripSeparator(),
                autoStartMenuItem
            ]);

            processMenuState = new ProcessMenuState
            {
                Config = config,
                Manager = manager,
                RootMenuItem = rootMenuItem,
                StartMenuItem = startMenuItem,
                StopMenuItem = stopMenuItem,
                ShowMenuItem = showMenuItem,
                HideMenuItem = hideMenuItem,
                AutoStartMenuItem = autoStartMenuItem
            };

            return processMenuState;
        }

        private void StartAutoStartProcesses()
        {
            foreach (var processMenuState in _processMenuStates.Where(x => x.Config.AutoStart))
            {
                try
                {
                    _logger.Info($"Process '{processMenuState.Config.Name}' autoStart is enabled; starting with NetTrayHost.");
                    processMenuState.Manager.Start();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Process '{processMenuState.Config.Name}' autoStart failed.", ex);
                }
            }
        }

        private void ToggleAutoStart(ProcessConfigModel config)
        {
            config.AutoStart = !config.AutoStart;
            _configLoader.Save(_config);
            _logger.Info($"Process '{config.Name}' autoStart changed to {config.AutoStart}.");

            var processMenuState = _processMenuStates.FirstOrDefault(x => ReferenceEquals(x.Config, config));
            if (processMenuState is not null)
            {
                UpdateProcessMenuState(processMenuState);
            }
        }

        private void ToggleNetTrayHostStartup()
        {
            var enabled = !_registryRunManager.IsEnabled();
            _registryRunManager.SetEnabled(enabled);
            UpdateNetTrayHostStartupMenuState();
        }

        private void UpdateNetTrayHostStartupMenuState()
        {
            _netTrayHostStartupMenuItem.Checked = _registryRunManager.IsEnabled();
        }

        private void UpdateProcessMenuState(ProcessMenuState processMenuState)
        {
            if (_trayIcon.ContextMenuStrip is { InvokeRequired: true })
            {
                _trayIcon.ContextMenuStrip.BeginInvoke(() => UpdateProcessMenuState(processMenuState));
                return;
            }

            var manager = processMenuState.Manager;
            var isRunning = manager.IsRunning;
            processMenuState.RootMenuItem.Text = isRunning
                ? $"{processMenuState.Config.Name}  Running"
                : $"{processMenuState.Config.Name}  Stopped";
            processMenuState.RootMenuItem.Image = isRunning ? _runningImage : _stoppedImage;
            processMenuState.StartMenuItem.Enabled = !isRunning;
            processMenuState.StopMenuItem.Enabled = isRunning;
            processMenuState.ShowMenuItem.Enabled = isRunning && manager.HasConsoleWindow;
            processMenuState.HideMenuItem.Enabled = isRunning && manager.HasConsoleWindow;
            processMenuState.AutoStartMenuItem.Checked = processMenuState.Config.AutoStart;
        }

        private static Image CreateStatusImage(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(color);
            using var pen = new Pen(Color.FromArgb(120, Color.Black));
            graphics.FillEllipse(brush, 3, 3, 10, 10);
            graphics.DrawEllipse(pen, 3, 3, 10, 10);
            return bitmap;
        }

        private void OnExit(object? sender, EventArgs e)
        {
            foreach (var processMenuState in _processMenuStates)
            {
                processMenuState.Manager.StopForHostShutdown();
                processMenuState.Manager.Dispose();
            }

            _runningImage.Dispose();
            _stoppedImage.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }
    }
}
