using Microsoft.Win32;

namespace NetTrayHost
{
    internal sealed class RegistryRunManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "NetTrayHost";

        private readonly AppLogger _logger;

        public RegistryRunManager(AppLogger logger)
        {
            _logger = logger;
        }

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(RunValueName) is string value && string.Equals(value, GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                    ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
                key.SetValue(RunValueName, GetExecutablePath(), RegistryValueKind.String);
                _logger.Info($"NetTrayHost Windows startup enabled. Registry value='{GetExecutablePath()}'.");
                return;
            }

            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
            {
                key?.DeleteValue(RunValueName, throwOnMissingValue: false);
            }

            _logger.Info("NetTrayHost Windows startup disabled.");
        }

        private static string GetExecutablePath()
        {
            return Environment.ProcessPath ?? Application.ExecutablePath;
        }
    }
}
