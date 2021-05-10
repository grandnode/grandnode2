using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Widgets.FacebookPixel.Models
{
    public class ConfigurationModel : BaseModel
    {
        public string StoreScope { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.PixelId")]
        public string PixelId { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.PixelScript")]
        public string PixelScript { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.AddToCartScript")]
        public string AddToCartScript { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.DetailsOrderScript")]
        public string DetailsOrderScript { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.AllowToDisableConsentCookie")]
        public bool AllowToDisableConsentCookie { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.ConsentDefaultState")]
        public bool ConsentDefaultState { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.ConsentName")]
        public string ConsentName { get; set; }

        [GrandResourceDisplayName("Widgets.FacebookPixel.ConsentDescription")]
        public string ConsentDescription { get; set; }

    }
}