using Grand.Domain.Configuration;

namespace Shipping.ByWeight
{
    public class ByWeightShippingSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }
        public int DisplayOrder { get; set; }

    }
}
