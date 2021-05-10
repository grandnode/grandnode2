using Grand.Domain.Configuration;
using System.Collections.Generic;

namespace Grand.Domain.Shipping
{
    public class ShippingProviderSettings : ISettings
    {
        public ShippingProviderSettings()
        {
            ActiveSystemNames = new List<string>();
        }

        /// <summary>
        /// Gets or sets system names of active Shipping rate  methods
        /// </summary>
        public List<string> ActiveSystemNames { get; set; }
    }
}
