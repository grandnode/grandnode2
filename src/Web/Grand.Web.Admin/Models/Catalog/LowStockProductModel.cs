using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class LowStockProductModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Name")]
        public string Name { get; set; }

        public string Attributes { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ManageInventoryMethod")]
        public string ManageInventoryMethod { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Published")]
        public bool Published { get; set; }
    }
}