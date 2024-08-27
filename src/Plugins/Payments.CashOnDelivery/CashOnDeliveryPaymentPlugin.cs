using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.CashOnDelivery;

/// <summary>
///     CashOnDelivery payment processor
/// </summary>
public class CashOnDeliveryPaymentPlugin(
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
        return CashOnDeliveryPaymentDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        var settings = new CashOnDeliveryPaymentSettings {
            DescriptionText =
                "<p>In cases where an order is placed, an authorized representative will contact you, personally or over telephone, to confirm the order.<br />After the order is confirmed, it will be processed.<br />Orders once confirmed, cannot be cancelled.</p><p>P.S. You can edit this text from admin panel.</p>"
        };
        await settingService.SaveSetting(settings);
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Payments.CashOnDelivery.FriendlyName", "Cash on delivery");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.DescriptionText", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.DescriptionText.Hint",
            "Enter info that will be shown to customers during checkout");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.PaymentMethodDescription", "Cash On Delivery");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.AdditionalFee", "Additional fee");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.AdditionalFee.Hint", "The additional fee.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.AdditionalFeePercentage", "Additional fee. Use percentage");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.AdditionalFeePercentage.Hint",
            "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.ShippableProductRequired", "Shippable product required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.ShippableProductRequired.Hint",
            "An option indicating whether shippable products are required in order to display this payment method during checkout.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.SkipPaymentInfo", "Skip payment info");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.CashOnDelivery.DisplayOrder", "Display order");


        await base.Install();
    }

    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<CashOnDeliveryPaymentSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.DescriptionText");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.DescriptionText.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.PaymentMethodDescription");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.AdditionalFee");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.AdditionalFee.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.AdditionalFeePercentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.AdditionalFeePercentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.ShippableProductRequired");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.ShippableProductRequired.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.CashOnDelivery.SkipPaymentInfo");

        await base.Uninstall();
    }

    #endregion
}