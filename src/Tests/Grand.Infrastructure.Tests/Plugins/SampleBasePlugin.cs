using Grand.Infrastructure.Plugins;

namespace Grand.Infrastructure.Tests.Plugins
{
    public class SampleBasePlugin : BasePlugin, IPlugin
    {
        private PluginInfo _pluginInfo = new() {
            Author = "grandnode",
            DisplayOrder = 0,
            FriendlyName = "sample plugin",
            SystemName = "SamplePlugin"
        };

        public override PluginInfo PluginInfo {
            get => _pluginInfo;
            set => _pluginInfo = value;
        }
        public override string ConfigurationUrl()
        {
            return "sample";
        }
    }
}
