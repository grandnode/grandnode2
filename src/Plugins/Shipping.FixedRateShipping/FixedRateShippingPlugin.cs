using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

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