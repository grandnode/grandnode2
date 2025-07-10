using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Order.ExternalOrderApi;

public class ExternalOrderApiPlugin : BasePlugin, IPlugin
{
    private readonly ISettingService _settingService;
    private readonly IPluginTranslateResource _pluginTranslateResource;

    public ExternalOrderApiPlugin(
        ISettingService settingService,
        IPluginTranslateResource pluginTranslateResource)
    {
        _settingService = settingService;
        _pluginTranslateResource = pluginTranslateResource;
    }
   
    public override async Task Install()
    {
        await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Order.ExternalOrderApi.FriendlyName", "External Order API");
        await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Order.ExternalOrderApi.Description", "API for accepting orders from external systems");
        
        await base.Install();
    }

    public override async Task Uninstall()
    {
        await base.Uninstall();
    }
}
