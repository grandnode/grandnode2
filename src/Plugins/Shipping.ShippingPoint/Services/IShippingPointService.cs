using Grand.Domain;
using Shipping.ShippingPoint.Domain;

namespace Shipping.ShippingPoint.Services;

/// <summary>
///     Store pickup point service interface
/// </summary>
public interface IShippingPointService
{
    /// <summary>
    ///     Gets all pickup points
    /// </summary>
    /// <param name="storeId">The store identifier; pass "" to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Pickup points</returns>
    Task<IPagedList<ShippingPoints>> GetAllStoreShippingPoint(string storeId = "", int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets a pickup point
    /// </summary>
    /// <param name="shippingPointId">Pickup point identifier</param>
    /// <returns>Pickup point</returns>
    Task<ShippingPoints> GetStoreShippingPointById(string shippingPointId);

    /// <summary>
    ///     Inserts a pickup point
    /// </summary>
    /// <param name="shippingPoint">Pickup point</param>
    Task InsertStoreShippingPoint(ShippingPoints shippingPoint);

    /// <summary>
    ///     Updates a pickup point
    /// </summary>
    /// <param name="shippingPoint">Pickup point</param>
    Task UpdateStoreShippingPoint(ShippingPoints shippingPoint);

    /// <summary>
    ///     Deletes a pickup point
    /// </summary>
    /// <param name="shippingPoint">Pickup point</param>
    Task DeleteStoreShippingPoint(ShippingPoints shippingPoint);
}