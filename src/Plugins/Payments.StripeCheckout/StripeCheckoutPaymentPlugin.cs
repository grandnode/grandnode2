using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.StripeCheckout;

/// <summary>
///     StripeCheckout payment processor
/// </summary>
public class StripeCheckoutPaymentPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return StripeCheckoutDefaults.ConfigurationUrl;
    }

    /// <summary>
    ///     Install the plugin
    /// </summary>
    public override async Task Install()
    {
        //settings
        await settingService.SaveSetting(new StripeCheckoutPaymentSettings {
            Description =
                "Enjoy seamless transactions with the flexibility to pay using your preferred payment method through Stripe Checkout. We ensure a secure and hassle-free payment experience, accommodating a wide range of payment options to suit your convenience.",
            DisplayOrder = 0,
            Line = "Order number {0}"
        });

        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.FriendlyName", "Pay with Stripe");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.Fields.ApiKey", "Stripe ApiKey (secret type)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.Fields.WebhookEndpointSecret", "Webhook secret for your endpoint");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.Fields.Description", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.Fields.Line", "Description line on the checkout page in Stripe");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.StripeCheckout.Fields.DisplayOrder", "Display order");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall the plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<StripeCheckoutPaymentSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.FriendlyName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.Fields.ApiKey");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.Fields.WebhookEndpointSecret");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.Fields.Description");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.Fields.Line");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.StripeCheckout.Fields.DisplayOrder");

        await base.Uninstall();
    }

    #endregion
}