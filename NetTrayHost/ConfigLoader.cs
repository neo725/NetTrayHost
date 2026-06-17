using System.Text.Json;
using NetTrayHost.Models;

namespace NetTrayHost
{
    internal sealed class ConfigLoader
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public ConfigLoader(string? configPath = null)
        {
            _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, "config.json");
        }

        public AppConfigModel Load()
        {
            if (!File.Exists(_configPath))
            {
                var config = CreateDefaultConfig();
                Save(config);
                return config;
            }

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfigModel>(json, _jsonOptions) ?? new AppConfigModel();
        }

        public void Save(AppConfigModel config)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }

        private static AppConfigModel CreateDefaultConfig()
        {
            var mockCliPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "spike", "MockCli", "bin", "Debug", "net8.0", "MockCli.exe"));

            return new AppConfigModel
            {
                Processes =
                [
                    new ProcessConfigModel
                    {
                        Name = "MockCli",
                        Exe = mockCliPath,
                        WorkingDirectory = Path.GetDirectoryName(mockCliPath) ?? AppContext.BaseDirectory,
                        Arguments = string.Empty,
                        AutoStart = false,
                        AutoRestart = true,
                        StartVisible = true
                    }
                ]
            };
        }
    }
}
