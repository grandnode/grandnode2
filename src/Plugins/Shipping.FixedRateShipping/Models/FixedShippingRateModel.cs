using Grand.Infrastructure.ModelBinding;

namespace Shipping.FixedRateShipping.Models
{
    public class FixedShippingRateModel
    {
        public string ShippingMethodId { get; set; }

        [GrandResourceDisplayName("Plugins.Shipping.FixedRateShipping.Fields.ShippingMethodName")]
        public string ShippingMethodName { get; set; }

        [GrandResourceDisplayName("Plugins.Shipping.FixedRateShipping.Fields.Rate")]
        public double Rate { get; set; }
    }
}