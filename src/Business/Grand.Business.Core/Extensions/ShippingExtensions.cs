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
        return shippingMethod.RestrictedCountries.Any(c => c.Id == countryId);
    }

    public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
        string roleId)
    {
        return shippingMethod.RestrictedGroups.Any(c => c == roleId);
    }

    public static bool CustomerGroupRestrictionExists(this ShippingMethod shippingMethod,
        IEnumerable<string> roleIds)
    {
        return shippingMethod.RestrictedGroups.Any(roleIds.Contains);
    }

    /// <summary>
    ///     The ids of the warehouses whose stock belongs to an external system; read once and reused across a batch, so
    ///     a bulk edit or import does not read the whole warehouse list per product
    /// </summary>
    public static async Task<ICollection<string>> ExternallyManagedWarehouseIds(this IWarehouseService warehouseService)
    {
        ArgumentNullException.ThrowIfNull(warehouseService);

        return (await warehouseService.GetAllWarehouses()).Where(x => x.ExternallyManaged).Select(x => x.Id)
            .ToList();
    }
}