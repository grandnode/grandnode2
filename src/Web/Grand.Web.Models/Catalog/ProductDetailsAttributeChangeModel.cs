using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class ProductDetailsAttributeChangeModel : BaseEntityModel
{
    public string Gtin { get; set; }
    public string Mpn { get; set; }
    public string Sku { get; set; }
    public string Price { get; set; }
    public string StockAvailability { get; set; }
    public bool DisplayOutOfStockSubscription { get; set; }
    public string ButtonTextOutOfStockSubscription { get; set; }
    public IList<string> EnabledAttributeMappingIds { get; set; } = new List<string>();
    public IList<string> DisabledAttributeMappingids { get; set; } = new List<string>();
    public IList<string> NotAvailableAttributeMappingids { get; set; } = new List<string>();
    public string PictureFullSizeUrl { get; set; }
    public string PictureDefaultSizeUrl { get; set; }
}