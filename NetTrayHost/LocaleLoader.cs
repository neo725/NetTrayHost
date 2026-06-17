using System.Text.Json;

namespace NetTrayHost
{
    internal sealed class LocaleLoader
    {
        private readonly Dictionary<string, string> _strings;

        public LocaleLoader(string locale)
        {
            var langDir = Path.Combine(AppContext.BaseDirectory, "lang");
            _strings =
                TryLoad(Path.Combine(langDir, $"{locale}.json")) ??
                TryLoad(Path.Combine(langDir, "en.json")) ??
                [];
        }

        public string this[string key] => _strings.TryGetValue(key, out var v) ? v : key;

        private static Dictionary<string, string>? TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
