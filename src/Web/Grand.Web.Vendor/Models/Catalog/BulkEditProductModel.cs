using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Catalog;

public class BulkEditProductModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.SKU")]

    public string Sku { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.Price")]
    public double Price { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.OldPrice")]
    public double OldPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.ManageInventoryMethod")]
    public int ManageInventoryMethodId { get; set; }

    public string ManageInventoryMethod { get; set; }


    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.StockQuantity")]
    public int StockQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.BulkEdit.Fields.Published")]
    public bool Published { get; set; }
}