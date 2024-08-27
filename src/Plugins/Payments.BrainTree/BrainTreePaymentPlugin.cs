using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.BrainTree;

public class BrainTreePaymentPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{

    #region Methods

    public override string ConfigurationUrl()
    {
        return BrainTreeDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        //settings
        var settings = new BrainTreePaymentSettings {
            UseSandBox = true,
            MerchantId = "",
            PrivateKey = "",
            PublicKey = ""
        };
        await settingService.SaveSetting(settings);

        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Payments.BrainTree.FriendlyName", "BrainTree payment");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.Use3DS", "Use the 3D secure");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.Use3DS.Hint", "Check to enable the 3D secure integration");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.DisplayOrder", "Display order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint",
            "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payments.BrainTree.PaymentMethodDescription", "Pay by credit / debit card");


        await base.Install();
    }

    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<BrainTreePaymentSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Payments.BrainTree.FriendlyName");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.UseSandbox");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.MerchantId");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.MerchantId.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.PublicKey");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.PublicKey.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.PrivateKey");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.AdditionalFee");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payments.BrainTree.PaymentMethodDescription");

        await base.Uninstall();
    }

    #endregion
}