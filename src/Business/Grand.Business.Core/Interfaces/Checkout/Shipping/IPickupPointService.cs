using Grand.Domain;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

public interface IPickupPointService
{
    /// <summary>
    ///     Gets a warehouse
    /// </summary>
    /// <param name="pickupPointId">The pickup point identifier</param>
    /// <returns>PickupPoint</returns>
    Task<PickupPoint> GetPickupPointById(string pickupPointId);

    /// <summary>
    ///     Gets all pickup points
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all pickup points</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>PickupPoints</returns>
    Task<IPagedList<PickupPoint>> GetAllPickupPoints(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets active pickup points
    /// </summary>
    /// <returns>PickupPoints</returns>
    Task<IList<PickupPoint>> LoadActivePickupPoints(string storeId = "");

    /// <summary>
    ///     Inserts a pickupPoint
    /// </summary>
    /// <param name="pickupPoint">PickupPoint</param>
    Task InsertPickupPoint(PickupPoint pickupPoint);

    /// <summary>
    ///     Updates the pickupPoint
    /// </summary>
    /// <param name="pickupPoint">PickupPoint</param>
    Task UpdatePickupPoint(PickupPoint pickupPoint);

    /// <summary>
    ///     Deletes a pickupPoint
    /// </summary>
    /// <param name="pickupPoint">The pickupPoint</param>
    Task DeletePickupPoint(PickupPoint pickupPoint);
}