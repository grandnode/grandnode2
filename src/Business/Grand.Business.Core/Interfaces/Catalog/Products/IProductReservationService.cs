using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

/// <summary>
///     Product reservation service interface
/// </summary>
public interface IProductReservationService
{
    /// <summary>
    ///     Adds product reservation
    /// </summary>
    /// <param name="productReservation">Product reservation</param>
    Task InsertProductReservation(ProductReservation productReservation);

    /// <summary>
    ///     Updates product reservation
    /// </summary>
    /// <param name="productReservation">Product reservation</param>
    Task UpdateProductReservation(ProductReservation productReservation);

    /// <summary>
    ///     Deletes a product reservation
    /// </summary>
    /// <param name="productReservation">Product reservation</param>
    Task DeleteProductReservation(ProductReservation productReservation);

    /// <summary>
    ///     Gets product reservations for product Id
    /// </summary>
    /// <param name="productId">Product Id</param>
    /// <param name="showVacant">Show vacant</param>
    /// <param name="date">Date</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Product reservations</returns>
    Task<IPagedList<ProductReservation>> GetProductReservationsByProductId(string productId, bool? showVacant,
        DateTime? date,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets product reservation for specified Id
    /// </summary>
    /// <param name="id">Product Id</param>
    /// <returns>Product reservation</returns>
    Task<ProductReservation> GetProductReservation(string id);

    /// <summary>
    ///     Adds customer reservations helper
    /// </summary>
    /// <param name="crh"></param>
    Task InsertCustomerReservationsHelper(CustomerReservationsHelper crh);

    /// <summary>
    ///     Deletes customer reservations helper
    /// </summary>
    /// <param name="crh"></param>
    Task DeleteCustomerReservationsHelper(CustomerReservationsHelper crh);

    /// <summary>
    ///     Cancel reservations by orderId
    /// </summary>
    /// <param name="orderId"></param>
    Task CancelReservationsByOrderId(string orderId);

    /// <summary>
    ///     Gets customer reservations helper by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>CustomerReservationsHelper</returns>
    Task<CustomerReservationsHelper> GetCustomerReservationsHelperById(string id);

    /// <summary>
    ///     Gets customer reservations helpers
    /// </summary>
    /// <param name="customerId">Customer ident</param>
    /// <returns>
    ///     List<CustomerReservationsHelper />
    /// </returns>
    Task<IList<CustomerReservationsHelper>> GetCustomerReservationsHelpers(string customerId);

    /// <summary>
    ///     Gets customer reservations helper by Shopping Cart Item id
    /// </summary>
    /// <param name="sciId">Sci ident</param>
    Task<IList<CustomerReservationsHelper>> GetCustomerReservationsHelperBySciId(string sciId);
}