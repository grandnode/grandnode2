using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Payments.PayPalStandard
{
    /// <summary>
    /// PayPalStandard payment processor
    /// </summary>
    public class PayPalStandardPaymentPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public PayPalStandardPaymentPlugin(
            ITranslationService translationService,
            ILanguageService languageService,
            ISettingService settingService)
        {
            _translationService = translationService;
            _languageService = languageService;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return PayPalStandardPaymentDefaults.ConfigurationUrl;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override async Task Install()
        {
            //settings
            await _settingService.SaveSetting(new PayPalStandardPaymentSettings
            {
                UseSandbox = true
            });

            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Payments.PayPalStandard.FriendlyName", "PayPal Standard");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFee", "Additional fee");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.BusinessEmail", "Business Email");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.BusinessEmail.Hint", "Specify your PayPal business email.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PassProductNamesAndTotals", "Pass product names and order totals to PayPal");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to PayPal.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PDTToken", "PDT Identity Token");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PDTValidateOrderTotal", "PDT. Validate order total");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PDTValidateOrderTotal.Hint", "Check if PDT handler should validate order totals.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.RedirectionTip", "You will be redirected to PayPal site to complete the order.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.UseSandbox", "Use Sandbox");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Instructions", "<p><b>If you're using this gateway ensure that your primary store currency is supported by PayPal.</b><br /><br />To use PDT, you must activate PDT and Auto Return in your PayPal account profile. You must also acquire a PDT identity token, which is used in all PDT communication you send to PayPal. Follow these steps to configure your account for PDT:<br /><br />1. Log in to your PayPal account (click <a href=\"https://www.paypal.com/us/webapps/mpp/referral/paypal-business-account2?partner_id=9JJPJNNPQ7PZ8\" target=\"_blank\">here</a> to create your account).<br />2. Click the Profile subtab.<br />3. Click Website Payment Preferences in the Seller Preferences column.<br />4. Under Auto Return for Website Payments, click the On radio button.<br />5. For the Return URL, enter the URL on your site that will receive the transaction ID posted by PayPal after a customer payment ({0}).<br />6. Under Payment Data Transfer, click the On radio button.<br />7. Click Save.<br />8. Click Website Payment Preferences in the Seller Preferences column.<br />9. Scroll down to the Payment Data Transfer section of the page to view your PDT identity token.<br /><br /></p>");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.PaymentMethodDescription", "You will be redirected to PayPal site to complete the payment");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.RoundingWarning", "It looks like you have \"ShoppingCartSettings.RoundPricesDuringCalculation\" setting disabled. Keep in mind that this can lead to a discrepancy of the order total amount, as PayPal only rounds to two doubles.");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.DisplayOrder", "Display order");


            await base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //settings
            await _settingService.DeleteSetting<PayPalStandardPaymentSettings>();

            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFee");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFee.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFeePercentage");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.AdditionalFeePercentage.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.BusinessEmail");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.BusinessEmail.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PassProductNamesAndTotals");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PassProductNamesAndTotals.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PDTToken");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.PDTToken.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.RedirectionTip");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.UseSandbox");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Fields.UseSandbox.Hint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.Instructions");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.PaymentMethodDescription");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Payments.PayPalStandard.RoundingWarning");

            await base.Uninstall();
        }

        #endregion

    }
}