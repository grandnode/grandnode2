using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Catalog
{
    public class DiscountModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Vendor.marketing.Discounts.Fields.Name")]
        public string Name { get; set; }
    }
}