namespace NetTrayHost
{
    internal sealed class AppLogger
    {
        private readonly object _lock = new();
        private readonly string _logPath;

        public AppLogger(string? logPath = null)
        {
            _logPath = logPath ?? Path.Combine(AppContext.BaseDirectory, "NetTrayHost.log");
        }

        public void Info(string message) => Write("INFO", message);

        public void Error(string message, Exception exception) => Write("ERROR", $"{message} {exception}");

        private void Write(string level, string message)
        {
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
    }
}
