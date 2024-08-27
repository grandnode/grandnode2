using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Tax.FixedRate;

/// <summary>
///     Fixed rate tax provider
/// </summary>
public class FixedRateTaxPlugin(
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return FixedRateTaxDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Tax.FixedRate.FriendlyName", "Tax by fixed rate");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.FixedRate.Fields.TaxCategoryName", "Tax category");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.FixedRate.Fields.Rate", "Rate");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Tax.FixedRate.FriendlyName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.FixedRate.Fields.TaxCategoryName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.FixedRate.Fields.Rate");

        await base.Uninstall();
    }
}