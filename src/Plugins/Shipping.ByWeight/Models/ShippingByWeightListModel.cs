using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Shipping.ByWeight.Models
{
    public class ShippingByWeightListModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Shipping.ByWeight.Fields.LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [GrandResourceDisplayName("Plugins.Shipping.ByWeight.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}