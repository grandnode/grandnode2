using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Extensions;

public static class ShippingExtensions
{
    public static bool IsShippingRateMethodActive(this IShippingRateCalculationProvider srcm,
        ShippingProviderSettings shippingProviderSettings)
    {
        ArgumentNullException.ThrowIfNull(srcm);
        ArgumentNullException.ThrowIfNull(shippingProviderSettings);

        return shippingProviderSettings.ActiveSystemNames != null && shippingProviderSettings.ActiveSystemNames.Any(
            activeMethodSystemName =>
                srcm.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool CountryRestrictionExists(this ShippingMethod shippingMethod,
        string countryId)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        var result = shippingMethod.RestrictedCountries.ToList().Find(c => c.Id == countryId) != null;
        return result;
    }

    public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
        string roleId)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        var result = shippingMethod.RestrictedGroups.ToList().Find(c => c == roleId) != null;
        return result;
    }

    public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
        List<string> roleIds)
    {
        ArgumentNullException.ThrowIfNull(shippingMethod);

        var result = shippingMethod.RestrictedGroups.ToList().Find(roleIds.Contains) != null;
        return result;
    }
}