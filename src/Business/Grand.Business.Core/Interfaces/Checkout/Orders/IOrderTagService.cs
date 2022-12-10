﻿using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders
{
    /// <summary>
    /// Order's tag service interface
    /// </summary>
    public interface IOrderTagService
    {
       
        /// <summary>
        /// Gets all order's tags
        /// </summary>
        /// <returns>order's tags</returns>
        Task<IList<OrderTag>> GetAllOrderTags();

        /// <summary>
        /// Gets order's tag
        /// </summary>
        /// <param name="orderTagId">order's tag identifier</param>
        /// <returns>order's tag</returns>
        Task<OrderTag> GetOrderTagById(string orderTagId);

        /// <summary>
        /// Gets order tag by name
        /// </summary>
        /// <param name="name">order tag name</param>
        /// <returns>order tag</returns>
        Task<OrderTag> GetOrderTagByName(string name);

        /// <summary>
        /// Inserts a order tag
        /// </summary>
        /// <param name="orderTag">order tag</param>
        Task InsertOrderTag(OrderTag orderTag);

        /// <summary>
        /// Update a order tag
        /// </summary>
        /// <param name="orderTag">order tag</param>
        Task UpdateOrderTag(OrderTag orderTag);
        /// <summary>
        /// Delete a order's tag
        /// </summary>
        /// <param name="orderTag">Order's tag</param>
        Task DeleteOrderTag(OrderTag orderTag);

        /// <summary>
        /// Assign a tag to the order
        /// </summary>
        /// <param name="orderTagId">order Tag</param>
        /// <param name="orderId"></param>
        Task AttachOrderTag(string orderTagId, string orderId);

        /// <summary>
        /// Detach a tag from the order
        /// </summary>
        /// <param name="orderTagId">order Tag</param>
        /// <param name="orderId"></param>
        Task DetachOrderTag(string orderTagId, string orderId);

        /// <summary>
        /// Get number of orders
        /// </summary>
        /// <param name="orderTagId">order tag identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Number of orders</returns>
        Task<int> GetOrderCount(string orderTagId, string storeId);
    }
}
