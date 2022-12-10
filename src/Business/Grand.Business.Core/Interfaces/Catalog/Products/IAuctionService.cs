﻿using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Auction service interface
    /// </summary>
    public interface IAuctionService
    {
       
        /// <summary>
        /// Inserts bid
        /// </summary>
        /// <param name="bid"></param>
        Task InsertBid(Bid bid);

        /// <summary>
        /// Updates bid
        /// </summary>
        /// <param name="bid"></param>
        Task UpdateBid(Bid bid);

        /// <summary>
        /// Deletes bid
        /// </summary>
        /// <param name="bid"></param>
        Task DeleteBid(Bid bid);

        /// <summary>
        /// Gets bids for product Id
        /// </summary>
        /// <param name="productId">Product Id</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns>Bids</returns>
        Task<IPagedList<Bid>> GetBidsByProductId(string productId, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets bids for Customer Id
        /// </summary>
        /// <param name="customerId">Customer Id</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns>Bids</returns>
        Task<IPagedList<Bid>> GetBidsByCustomerId(string customerId, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets bid for specified Id
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>Bid</returns>
        Task<Bid> GetBid(string id);

        /// <summary>
        /// Get latest bid for product Id
        /// </summary>
        /// <param name="productId">productId</param>
        /// <returns>Bid</returns>
        Task<Bid> GetLatestBid(string productId);

        /// <summary>
        /// Updates highest bid
        /// </summary>
        /// <param name="product"></param>
        /// <param name="bid"></param>
        /// <param name="bidder"></param>
        Task UpdateHighestBid(Product product, double bid, string bidder);

        /// <summary>
        /// Updates auction ended
        /// </summary>
        /// <param name="product"></param>
        /// <param name="ended"></param>
        /// <param name="endDate"></param>
        Task UpdateAuctionEnded(Product product, bool ended, bool endDate = false);

        /// <summary>
        /// Gets auctions that have to be ended
        /// </summary>
        Task<IList<Product>> GetAuctionsToEnd();

        /// <summary>
        /// New bid
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="store"></param>
        /// <param name="warehouseId"></param>
        /// <param name="language"></param>
        /// <param name="amount"></param>
        Task NewBid(Customer customer, Product product, Store store, Language language, string warehouseId, double amount);

        /// <summary>
        /// Cancel bid for product
        /// </summary>
        /// <param name="orderId">order id</param>
        Task CancelBidByOrder(string orderId);
    }
}