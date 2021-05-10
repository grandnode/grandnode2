using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Business.Checkout.Extensions
{
    public static class ShippingExtensions
    {
        public static bool IsShippingRateMethodActive(this IShippingRateCalculationProvider srcm,
            ShippingProviderSettings shippingProviderSettings)
        {
            if (srcm == null)
                throw new ArgumentNullException(nameof(srcm));

            if (shippingProviderSettings == null)
                throw new ArgumentNullException(nameof(shippingProviderSettings));

            if (shippingProviderSettings.ActiveSystemNames == null)
                return false;
            foreach (string activeMethodSystemName in shippingProviderSettings.ActiveSystemNames)
                if (srcm.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public static bool CountryRestrictionExists(this ShippingMethod shippingMethod,
            string countryId)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            bool result = shippingMethod.RestrictedCountries.ToList().Find(c => c.Id == countryId) != null;
            return result;
        }
        public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
           string roleId)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            bool result = shippingMethod.RestrictedGroups.ToList().Find(c => c == roleId) != null;
            return result;
        }

        public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
           List<string> roleIds)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            bool result = shippingMethod.RestrictedGroups.ToList().Find(c => roleIds.Contains(c)) != null;
            return result;
        }
    }
}
