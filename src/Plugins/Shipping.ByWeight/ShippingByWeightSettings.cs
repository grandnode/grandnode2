
using Grand.Domain.Configuration;

namespace Shipping.ByWeight
{
    public class ShippingByWeightSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }
    }
}