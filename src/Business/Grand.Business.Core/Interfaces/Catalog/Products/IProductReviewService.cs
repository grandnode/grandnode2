﻿using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Product review service
    /// </summary>
    public interface IProductReviewService
    {
        /// <summary>
        /// Insert product review 
        /// </summary>
        /// <param name="productReview">Product review</param>
        Task InsertProductReview(ProductReview productReview);

        /// <summary>
        /// Update product review 
        /// </summary>
        /// <param name="productReview">Product review</param>
        Task UpdateProductReview(ProductReview productReview);

        /// <summary>
        /// Deletes a product review
        /// </summary>
        /// <param name="productReview">Product review</param>
        Task DeleteProductReview(ProductReview productReview);

        /// <summary>
        /// Gets all product reviews
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <param name="approved">A value indicating whether to content is approved; null to load all records</param> 
        /// <param name="fromUtc">Item creation from; null to load all records</param>
        /// <param name="toUtc">Item item creation to; null to load all records</param>
        /// <param name="message">Search title or review text; null to load all records</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="productId">Product identifier; "" to load all records</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns>Reviews</returns>
        Task<IPagedList<ProductReview>> GetAllProductReviews(string customerId, bool? approved = null,
            DateTime? fromUtc = null, DateTime? toUtc = null,
            string message = null, string storeId = "", string productId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets product review
        /// </summary>
        /// <param name="productReviewId">Product review identifier</param>
        /// <returns>Product review</returns>
        Task<ProductReview> GetProductReviewById(string productReviewId);

        
    }
}
