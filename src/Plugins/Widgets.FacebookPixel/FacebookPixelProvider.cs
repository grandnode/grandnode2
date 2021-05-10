using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Widgets.FacebookPixel
{
    public class FacebookPixelProvider : IWidgetProvider
    {
        private readonly FacebookPixelSettings _facebookPixelSettings;
        private readonly ITranslationService _translationService;

        public FacebookPixelProvider(
            FacebookPixelSettings facebookPixelSettings,
            ITranslationService translationService)
        {
            _facebookPixelSettings = facebookPixelSettings;
            _translationService = translationService;
        }

        public string ConfigurationUrl => FacebookPixelDefaults.ConfigurationUrl;

        public string SystemName => FacebookPixelDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(FacebookPixelDefaults.FriendlyName);

        public int Priority => _facebookPixelSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public async Task<IList<string>> GetWidgetZones()
        {
            return await Task.FromResult(new List<string>
            {
                FacebookPixelDefaults.Page, FacebookPixelDefaults.AddToCart, FacebookPixelDefaults.OrderDetails
            });
        }

        public Task<string> GetPublicViewComponentName(string widgetZone)
        {
            return Task.FromResult("WidgetsFacebookPixel");
        }

    }
}
