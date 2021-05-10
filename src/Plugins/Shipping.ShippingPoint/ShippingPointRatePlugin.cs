using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Shipping.ShippingPoint
{
    public class ShippingPointRatePlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        #endregion

        #region Ctor
        public ShippingPointRatePlugin(
            ITranslationService translationService,
            ILanguageService languageService
            )
        {
            _translationService = translationService;
            _languageService = languageService;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            //locales       
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.FriendlyName", "Shipping Point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.PluginName", "Shipping Point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.PluginDescription", "Choose a place where you can pick up your order");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.ShippingPointName", "Point Name");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Description", "Description");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.PickupFee", "Pickup Fee");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.OpeningHours", "Open Between");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Store", "Store Name");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.City", "City");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Address1", "Address 1");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.ZipPostalCode", "Zip postal code");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Country", "Country");

            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.ShippingPointName", "Point Name");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.Address", "Address");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.MethodAndFee", "{0} ({1})");

            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.AddNew", "Add New Point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredShippingPointName", "Shipping Point Name Is Required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredDescription", "Description Is Required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredOpeningHours", "Opening Hours Are Required");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.SelectShippingOption", "Select Shipping Option");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.ChooseShippingPoint", "Choose Shipping Point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.ShippingPoint.SelectBeforeProceed", "Select Shipping Option Before Proceed");

            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.PluginName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.PluginDescription");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.ShippingPointName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Description");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.PickupFee");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.OpeningHours");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Store");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.AddNew");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredShippingPointName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredDescription");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.RequiredOpeningHours");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.SelectShippingOption");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.ChooseShippingPoint");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.SelectBeforeProceed");

            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.City");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Address1");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.ZipPostalCode");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Fields.Country");

            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.ShippingPointName");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.Address");
            await this.DeletePluginTranslationResource(_translationService, _languageService, "Shipping.ShippingPoint.MethodAndFee");


            await base.Uninstall();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return ShippingPointRateDefaults.ConfigurationUrl;
        }

        #endregion

    }
}
