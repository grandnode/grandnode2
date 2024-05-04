using Grand.Infrastructure.Plugins;

namespace Grand.Infrastructure.Tests.Plugins;

public class SampleBasePlugin : BasePlugin, IPlugin
{
    public override PluginInfo PluginInfo { get; set; } = new() {
        Author = "grandnode",
        DisplayOrder = 0,
        FriendlyName = "sample plugin",
        SystemName = "SamplePlugin"
    };

    public override string ConfigurationUrl()
    {
        return "sample";
    }
}