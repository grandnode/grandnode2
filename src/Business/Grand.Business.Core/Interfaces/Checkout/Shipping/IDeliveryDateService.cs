using Grand.Domain;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

public interface IDeliveryDateService
{
    /// <summary>
    ///     Gets a delivery date
    /// </summary>
    /// <param name="deliveryDateId">The delivery date identifier</param>
    /// <returns>Delivery date</returns>
    Task<DeliveryDate> GetDeliveryDateById(string deliveryDateId);

    /// <summary>
    ///     Gets all delivery dates
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all delivery dates</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Delivery dates</returns>
    Task<IPagedList<DeliveryDate>> GetAllDeliveryDates(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Inserts a delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    Task InsertDeliveryDate(DeliveryDate deliveryDate);

    /// <summary>
    ///     Updates the delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    Task UpdateDeliveryDate(DeliveryDate deliveryDate);

    /// <summary>
    ///     Deletes a delivery date
    /// </summary>
    /// <param name="deliveryDate">The delivery date</param>
    Task DeleteDeliveryDate(DeliveryDate deliveryDate);
}