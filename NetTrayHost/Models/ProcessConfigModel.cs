namespace NetTrayHost.Models
{
    internal sealed class ProcessConfigModel
    {
        public string Name { get; set; } = string.Empty;
        public string Exe { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool AutoStart { get; set; }
        public bool AutoRestart { get; set; }
        public int MaxAutoRestartAttempts { get; set; } = 3;
        public bool StartVisible { get; set; }
    }
}
