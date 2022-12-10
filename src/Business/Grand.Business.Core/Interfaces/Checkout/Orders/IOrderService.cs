using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Orders
{
    /// <summary>
    /// Order service interface
    /// </summary>
    public interface IOrderService
    {
        #region Orders

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>Order</returns>
        Task<Order> GetOrderById(string orderId);

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderItemId">The order item identifier</param>
        /// <returns>Order</returns>
        Task<Order> GetOrderByOrderItemId(string orderItemId);

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderNumber">The order number</param>
        /// <returns>Order</returns>
        Task<Order> GetOrderByNumber(int orderNumber);

        /// <summary>
        /// Gets orders by code
        /// </summary>
        /// <param name="code">The order code</param>
        /// <returns>Order</returns>
        Task<IList<Order>> GetOrdersByCode(string code);

        /// <summary>
        /// Get orders by identifiers
        /// </summary>
        /// <param name="orderIds">Order identifiers</param>
        /// <returns>Order</returns>
        Task<IList<Order>> GetOrdersByIds(string[] orderIds);

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderGuid">The order identifier</param>
        /// <returns>Order</returns>
        Task<Order> GetOrderByGuid(Guid orderGuid);

        /// <summary>
        /// Search orders
        /// </summary>
        /// <param name="storeId">Store identifier; null to load all orders</param>
        /// <param name="vendorId">Vendor identifier; null to load all orders</param>
        /// <param name="customerId">Customer identifier; null to load all orders</param>
        /// <param name="productId">Product identifier which was purchased in an order; 0 to load all orders</param>
        /// <param name="affiliateId">Affiliate identifier; 0 to load all orders</param>
        /// <param name="warehouseId">Warehouse identifier, only orders with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="billingCountryId">Billing country identifier; 0 to load all orders</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="salesEmployeeId">Sales ident</param>
        /// <param name="paymentMethodSystemName">Payment method system name; null to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="os">Order status; null to load all orders</param>
        /// <param name="ps">Order payment status; null to load all orders</param>
        /// <param name="ss">Order shipment status; null to load all orders</param>
        /// <param name="billingEmail">Billing email. Leave empty to load all records.</param>
        /// <param name="billingLastName"></param>
        /// <param name="orderGuid">Search by order GUID (Global unique identifier) or part of GUID. Leave empty to load all records.</param>
        /// <param name="orderCode">Search by order code.</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="orderTagId">Order tag identifier</param>
        /// <returns>Orders</returns>
        Task<IPagedList<Order>> SearchOrders(string storeId = "",
            string vendorId = "", string customerId = "",
            string productId = "", string affiliateId = "", string warehouseId = "",
            string billingCountryId = "", string ownerId = "", string salesEmployeeId = "", string paymentMethodSystemName = null,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
            string billingEmail = null, string billingLastName = "", string orderGuid = null,
            string orderCode = null, int pageIndex = 0, int pageSize = int.MaxValue, string orderTagId = "");
        
        /// <summary>
        /// Inserts an order
        /// </summary>
        /// <param name="order">Order</param>
        Task InsertOrder(Order order);

        /// <summary>
        /// Updates the order
        /// </summary>
        /// <param name="order">The order</param>
        Task UpdateOrder(Order order);

        #endregion

        #region Orders items

        /// <summary>
        /// Gets an order item
        /// </summary>
        /// <param name="orderItemGuid">Order item identifier</param>
        /// <returns>Order item</returns>
        Task<OrderItem> GetOrderItemByGuid(Guid orderItemGuid);

        /// <summary>
        /// Delete an order item
        /// </summary>
        /// <param name="orderItem">The order item</param>
        Task DeleteOrderItem(OrderItem orderItem);

        #endregion

        #region Order notes

        /// <summary>
        /// Deletes an order note
        /// </summary>
        /// <param name="orderNote">The order note</param>
        Task DeleteOrderNote(OrderNote orderNote);

        /// <summary>
        /// Insert an order note
        /// </summary>
        /// <param name="orderNote">The order note</param>
        Task InsertOrderNote(OrderNote orderNote);


        /// <summary>
        /// Get order notes for order
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <returns>OrderNote</returns>
        Task<IList<OrderNote>> GetOrderNotes(string orderId);

        /// <summary>
        /// Get order note by id
        /// </summary>
        /// <param name="orderNoteId">Order note identifier</param>
        /// <returns>OrderNote</returns>
        Task<OrderNote> GetOrderNote(string orderNoteId);


        /// <summary>
        /// Cancel Expired UnPaid Orders
        /// </summary>
        /// <param name="expirationDateUtc">Date at which all unPaid  orders and has pending status Would be Canceled</param>
        Task CancelExpiredOrders(DateTime expirationDateUtc);

        #endregion

    }
}
