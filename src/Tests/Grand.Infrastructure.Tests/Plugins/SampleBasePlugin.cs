using Grand.Infrastructure.Plugins;

namespace Grand.Infrastructure.Tests.Plugins
{
    public class SampleBasePlugin : BasePlugin, IPlugin
    {
        private PluginInfo _pluginInfo;
        public SampleBasePlugin()
        {
            _pluginInfo = new PluginInfo() {
                Author = "grandnode",
                DisplayOrder = 0,
                FriendlyName = "sample plugin",
                SystemName = "SamplePlugin"
            };
        }
        public override PluginInfo PluginInfo {
            get {
                return _pluginInfo;
            }
            set {
                _pluginInfo = value;
            }
        }
        public override string ConfigurationUrl()
        {
            return "sample";
        }
    }
}
