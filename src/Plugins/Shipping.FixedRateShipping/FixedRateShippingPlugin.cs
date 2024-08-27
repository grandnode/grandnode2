using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Shipping.FixedRateShipping;

/// <summary>
///     Fixed rate shipping computation method
/// </summary>
public class FixedRateShippingPlugin(
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Install plugin
    /// </summary>
    public override async Task Install()
    {
        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.FixedRate.FriendlyName", "Shipping fixed rate");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.FixedRateShipping.Fields.ShippingMethodName", "Shipping method");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.FixedRateShipping.Fields.Rate", "Rate");

        await base.Install();
    }


    /// <summary>
    ///     Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Shipping.FixedRateShipping.Fields.ShippingMethodName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Shipping.FixedRateShipping.Fields.Rate");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.FixedRate.FriendlyName");

        await base.Uninstall();
    }

    #endregion
}