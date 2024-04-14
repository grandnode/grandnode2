using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class LowStockProductModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Name")]
    public string Name { get; set; }

    public string Attributes { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ManageInventoryMethod")]
    public string ManageInventoryMethod { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.StockQuantity")]
    public int StockQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Published")]
    public bool Published { get; set; }
}