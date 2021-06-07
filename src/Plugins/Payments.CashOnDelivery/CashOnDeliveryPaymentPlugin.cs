using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Payments.CashOnDelivery
{
    /// <summary>
    /// CashOnDelivery payment processor
    /// </summary>
    public class CashOnDeliveryPaymentPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        #endregion

        #region Ctor

        public CashOnDeliveryPaymentPlugin(
            ISettingService settingService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _settingService = settingService;
            _translationService = translationService;
            _languageService = languageService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return CashOnDeliveryPaymentDefaults.ConfigurationUrl;
        }

        public override async Task Install()
        {
            var settings = new CashOnDeliveryPaymentSettings
            {
                DescriptionText = "<p>In cases where an order is placed, an authorized representative will contact you, personally or over telephone, to confirm the order.<br />After the order is confirmed, it will be processed.<br />Orders once confirmed, cannot be cancelled.</p><p>P.S. You can edit this text from admin panel.</p>"
            };
            await _settingService.SaveSetting(settings);
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Payments.CashOnDelivery.FriendlyName", "Cash on delivery");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.DescriptionText", "Description");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.DescriptionText.Hint", "Enter info that will be shown to customers during checkout");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.PaymentMethodDescription", "Cash On Delivery");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFee", "Additional fee");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFee.Hint", "The additional fee.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFeePercentage", "Additional fee. Use percentage");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.ShippableProductRequired", "Shippable product required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.ShippableProductRequired.Hint", "An option indicating whether shippable products are required in order to display this payment method during checkout.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.DisplayOrder", "Display order");


            await base.Install();
        }

        public override async Task Uninstall()
        {
            //settings
            await _settingService.DeleteSetting<CashOnDeliveryPaymentSettings>();

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.DescriptionText");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.DescriptionText.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.PaymentMethodDescription");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFee");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFee.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFeePercentage");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.AdditionalFeePercentage.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.ShippableProductRequired");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payment.CashOnDelivery.ShippableProductRequired.Hint");

            await base.Uninstall();
        }

        #endregion


    }
}