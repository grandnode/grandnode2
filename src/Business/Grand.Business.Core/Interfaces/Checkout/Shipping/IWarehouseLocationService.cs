using Grand.Domain;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

/// <summary>
///     The places in the warehouses where batches and serial numbers are kept
/// </summary>
public interface IWarehouseLocationService
{
    Task<WarehouseLocation> GetLocationById(string id);

    /// <summary>
    ///     The locations of the warehouses by display order and code
    /// </summary>
    /// <param name="warehouseIds">The warehouses; null for all of them</param>
    Task<IPagedList<WarehouseLocation>> GetLocations(IList<string> warehouseIds = null, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     The locations of the ids, in no order; a deleted one is missing
    /// </summary>
    Task<IList<WarehouseLocation>> GetLocationsByIds(IList<string> ids);

    Task InsertLocation(WarehouseLocation location);

    Task UpdateLocation(WarehouseLocation location);

    /// <summary>
    ///     Deletes the location; the batches and serial numbers kept there are left without one
    /// </summary>
    Task DeleteLocation(WarehouseLocation location);
}
