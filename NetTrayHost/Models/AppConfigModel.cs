namespace NetTrayHost.Models
{
    internal sealed class AppConfigModel
    {
        public string Locale { get; set; } = "zh-TW";
        public List<ProcessConfigModel> Processes { get; set; } = [];
    }
}
