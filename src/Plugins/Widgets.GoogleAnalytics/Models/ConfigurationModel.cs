using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Widgets.GoogleAnalytics.Models
{
    public class ConfigurationModel : BaseModel
    {
        public string StoreScope { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.GoogleId")]
        public string GoogleId { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.TrackingScript")]
        //tracking code
        public string TrackingScript { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.EcommerceScript")]
        public string EcommerceScript { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.EcommerceDetailScript")]
        public string EcommerceDetailScript { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.IncludingTax")]
        public bool IncludingTax { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.AllowToDisableConsentCookie")]
        public bool AllowToDisableConsentCookie { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.ConsentDefaultState")]
        public bool ConsentDefaultState { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.ConsentName")]
        public string ConsentName { get; set; }

        [GrandResourceDisplayName("Widgets.GoogleAnalytics.ConsentDescription")]
        public string ConsentDescription { get; set; }

    }
}