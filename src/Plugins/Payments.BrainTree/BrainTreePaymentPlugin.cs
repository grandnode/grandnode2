using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Payments.BrainTree
{
    public class BrainTreePaymentPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public BrainTreePaymentPlugin(
            ISettingService settingService,
            ILanguageService languageService,
            ITranslationService translationService)
        {
            _settingService = settingService;
            _translationService = translationService;
            _languageService = languageService;
        }

        #endregion

        #region Methods


        public override string ConfigurationUrl()
        {
            return BrainTreeDefaults.ConfigurationUrl;
        }

        public override async Task Install()
        {
            //settings
            var settings = new BrainTreePaymentSettings
            {
                UseSandBox = true,
                MerchantId = "",
                PrivateKey = "",
                PublicKey = ""
            };
            await _settingService.SaveSetting(settings);

            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Payments.BrainTree.FriendlyName", "BrainTree payment");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.Use3DS", "Use the 3D secure");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.Use3DS.Hint", "Check to enable the 3D secure integration");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.UseSandbox", "Use Sandbox");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.MerchantId", "Merchant ID");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.MerchantId.Hint", "Enter Merchant ID");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PublicKey", "Public Key");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PublicKey.Hint", "Enter Public key");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PrivateKey", "Private Key");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PrivateKey.Hint", "Enter Private key");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFee", "Additional fee");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.DisplayOrder", "Display order");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.BrainTree.PaymentMethodDescription", "Pay by credit / debit card");

            
            await base.Install();
        }

        public override async Task Uninstall()
        {
            //settings
            await _settingService.DeleteSetting<BrainTreePaymentSettings>();

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Payments.BrainTree.FriendlyName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.UseSandbox");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.UseSandbox.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.MerchantId");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.MerchantId.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PublicKey");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PublicKey.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PrivateKey");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.PrivateKey.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFee");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFee.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.BrainTree.PaymentMethodDescription");

            await base.Uninstall();
        }

        #endregion

    }
}
