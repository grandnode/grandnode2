using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class BulkEditProductModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.SKU")]

        public string Sku { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.Price")]
        public double Price { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.OldPrice")]
        public double OldPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.ManageInventoryMethod")]
        public string ManageInventoryMethod { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.Fields.Published")]
        public bool Published { get; set; }
    }
}