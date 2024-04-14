using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Catalog;

public class CopyProductModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.Copy.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Copy.CopyImages")]
    public bool CopyImages { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Copy.Published")]
    public bool Published { get; set; }
}