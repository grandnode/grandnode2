using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Extensions
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

            return shippingProviderSettings.ActiveSystemNames != null && shippingProviderSettings.ActiveSystemNames.Any(activeMethodSystemName => srcm.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool CountryRestrictionExists(this ShippingMethod shippingMethod,
            string countryId)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            var result = shippingMethod.RestrictedCountries.ToList().Find(c => c.Id == countryId) != null;
            return result;
        }
        public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
           string roleId)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            var result = shippingMethod.RestrictedGroups.ToList().Find(c => c == roleId) != null;
            return result;
        }

        public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
           List<string> roleIds)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            var result = shippingMethod.RestrictedGroups.ToList().Find(roleIds.Contains) != null;
            return result;
        }
    }
}
