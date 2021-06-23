using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Widgets.Slider
{
    public class SliderWidgetProvider : IWidgetProvider
    {
        private readonly ITranslationService _translationService;
        private readonly SliderWidgetSettings _sliderWidgetSettings;

        public SliderWidgetProvider(ITranslationService translationService, SliderWidgetSettings sliderWidgetSettings)
        {
            _translationService = translationService;
            _sliderWidgetSettings = sliderWidgetSettings;
        }

        public string ConfigurationUrl => SliderWidgetDefaults.ConfigurationUrl;

        public string SystemName => SliderWidgetDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(SliderWidgetDefaults.FriendlyName);

        public int Priority => _sliderWidgetSettings.DisplayOrder;

        public IList<string> LimitedToStores => _sliderWidgetSettings.LimitedToStores;

        public IList<string> LimitedToGroups => _sliderWidgetSettings.LimitedToGroups;

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public async Task<IList<string>> GetWidgetZones()
        {
            return await Task.FromResult(new List<string>
            {
                SliderWidgetDefaults.WidgetZoneHomePage,
                SliderWidgetDefaults.WidgetZoneCategoryPage,
                SliderWidgetDefaults.WidgetZoneCollectionPage,
                SliderWidgetDefaults.WidgetZoneBrandPage,
            });
        }

        public Task<string> GetPublicViewComponentName(string widgetZone)
        {
            return Task.FromResult("WidgetSlider");
        }

    }
}
