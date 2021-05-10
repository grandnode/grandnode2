using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Tax.FixedRate
{
    /// <summary>
    /// Fixed rate tax provider
    /// </summary>
    public class FixedRateTaxPlugin : BasePlugin, IPlugin
    {
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        public FixedRateTaxPlugin(ITranslationService translationService, ILanguageService languageService)
        {
            _translationService = translationService;
            _languageService = languageService;
        }


        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return FixedRateTaxDefaults.ConfigurationUrl;
        }

        public override async Task Install()
        {
            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Tax.FixedRate.FriendlyName", "Tax by fixed rate");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Tax.FixedRate.Fields.TaxCategoryName", "Tax category");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Tax.FixedRate.Fields.Rate", "Rate");

            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //locales
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Tax.FixedRate.Fields.TaxCategoryName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Plugins.Tax.FixedRate.Fields.Rate");

            await base.Uninstall();
        }
    }
}
