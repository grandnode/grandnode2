using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Shipping.FixedRateShipping
{
    /// <summary>
    /// Fixed rate shipping computation method
    /// </summary>
    public class FixedRateShippingPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        #endregion

        #region Ctor
        public FixedRateShippingPlugin(
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _translationService = translationService;
            _languageService = languageService;
        }
        #endregion

        #region Utilities


        #endregion

        #region Methods

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return FixedRateShippingDefaults.ConfigurationUrl;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.FixedRate.FriendlyName", "Shipping fixed rate");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.FixedRateShipping.Fields.ShippingMethodName", "Shipping method");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.FixedRateShipping.Fields.Rate", "Rate");

            await base.Install();
        }


        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Shipping.FixedRateShipping.Fields.ShippingMethodName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Shipping.FixedRateShipping.Fields.Rate");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.FixedRate.FriendlyName");

            await base.Uninstall();
        }


        #endregion

    }
}