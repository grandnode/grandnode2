using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Widgets.GoogleAnalytics
{
    /// <summary>
    /// Live person provider
    /// </summary>
    public class GoogleAnalyticProvider : IWidgetProvider
    {
        private readonly ITranslationService _translationService;
        private readonly GoogleAnalyticsEcommerceSettings _googleAnalyticsEcommerceSettings;

        public GoogleAnalyticProvider(
            ITranslationService translationService,
            GoogleAnalyticsEcommerceSettings googleAnalyticsEcommerceSettings)
        {
            _translationService = translationService;
            _googleAnalyticsEcommerceSettings = googleAnalyticsEcommerceSettings;
        }

        public string ConfigurationUrl => GoogleAnalyticDefaults.ConfigurationUrl;

        public string SystemName => GoogleAnalyticDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(GoogleAnalyticDefaults.FriendlyName);

        public int Priority => _googleAnalyticsEcommerceSettings.DisplayOrder;

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
                "body_end_html_tag_before", "clean_body_end_html_tag_before"
            });
        }

        public Task<string> GetPublicViewComponentName(string widgetZone)
        {
            return Task.FromResult("WidgetsGoogleAnalytics");
        }
    }
}
