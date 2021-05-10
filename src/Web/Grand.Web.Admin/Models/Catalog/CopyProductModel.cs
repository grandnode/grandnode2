using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class CopyProductModel : BaseEntityModel
    {

        [GrandResourceDisplayName("Admin.Catalog.Products.Copy.Name")]
        
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Copy.CopyImages")]
        public bool CopyImages { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Copy.Published")]
        public bool Published { get; set; }

    }
}