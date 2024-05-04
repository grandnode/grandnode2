using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.StripeCheckout;

/// <summary>
///     StripeCheckout payment processor
/// </summary>
public class StripeCheckoutPaymentPlugin : BasePlugin, IPlugin
{
    #region Ctor

    public StripeCheckoutPaymentPlugin(
        ITranslationService translationService,
        ILanguageService languageService,
        ISettingService settingService)
    {
        _translationService = translationService;
        _languageService = languageService;
        _settingService = settingService;
    }

    #endregion

    #region Fields

    private readonly ITranslationService _translationService;
    private readonly ILanguageService _languageService;
    private readonly ISettingService _settingService;

    #endregion

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
        await _settingService.SaveSetting(new StripeCheckoutPaymentSettings {
            Description =
                "Enjoy seamless transactions with the flexibility to pay using your preferred payment method through Stripe Checkout. We ensure a secure and hassle-free payment experience, accommodating a wide range of payment options to suit your convenience.",
            DisplayOrder = 0,
            Line = "Order number {0}"
        });

        //locales
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.FriendlyName", "Pay with Stripe");
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.ApiKey", "Stripe ApiKey (secret type)");
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.WebhookEndpointSecret", "Webhook secret for your endpoint");
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.Description", "Description");
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.Line", "Description line on the checkout page in Stripe");
        await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.DisplayOrder", "Display order");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall the plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //settings
        await _settingService.DeleteSetting<StripeCheckoutPaymentSettings>();

        //locales
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.FriendlyName");
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.ApiKey");
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.WebhookEndpointSecret");
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.Description");
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.Line");
        await this.DeletePluginTranslationResource(_translationService, _languageService,
            "Plugins.Payments.StripeCheckout.Fields.DisplayOrder");

        await base.Uninstall();
    }

    #endregion
}