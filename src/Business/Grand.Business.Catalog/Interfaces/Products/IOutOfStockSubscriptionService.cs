using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Products
{
    /// <summary>
    /// Out of stock subscription service interface
    /// </summary>
    public partial interface IOutOfStockSubscriptionService
    {
                /// <summary>
        /// Gets all subscriptions
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="storeId">Store identifier; pass "" to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Subscriptions</returns>
        Task<IPagedList<OutOfStockSubscription>> GetAllSubscriptionsByCustomerId(string customerId,
            string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets all subscriptions
        /// </summary>
        /// <param name="customerId">Customer id</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="attributes">Attribute</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>Subscriptions</returns>
        Task<OutOfStockSubscription> FindSubscription(string customerId, string productId, IList<CustomAttribute> attributes, string storeId, string warehouseId);

        /// <summary>
        /// Gets a subscription
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <returns>Subscription</returns>
        Task<OutOfStockSubscription> GetSubscriptionById(string subscriptionId);

        /// <summary>
        /// Inserts subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        Task InsertSubscription(OutOfStockSubscription subscription);

        /// <summary>
        /// Updates subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        Task UpdateSubscription(OutOfStockSubscription subscription);

        /// <summary>
        /// Delete a out of stock subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        Task DeleteSubscription(OutOfStockSubscription subscription);
        /// <summary>
        /// Send notification to subscribers
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouse">Warehouse ident</param>
        /// <returns>Number of sent email</returns>
        Task SendNotificationsToSubscribers(Product product, string warehouse);

        /// <summary>
        /// Send notification to subscribers
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attribute</param>
        /// <param name="warehouse">Warehouse ident</param>
        /// <returns>Number of sent email</returns>
        Task SendNotificationsToSubscribers(Product product, IList<CustomAttribute> attributes, string warehouse);
    }
}
