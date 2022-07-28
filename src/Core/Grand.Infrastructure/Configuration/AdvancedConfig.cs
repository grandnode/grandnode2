namespace Grand.Infrastructure.Configuration
{
    public partial class AdvancedConfig
    {
        public string DbConnectionString { get; set; }
        public int DbProvider { get; set; }
        public IList<string> InstalledPlugins { get; set; } = new List<string>();

    }
}
