using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Recently viewed products service
    /// </summary>
    public interface IRecentlyViewedProductsService
    {
        /// <summary>
        /// Gets a "recently viewed products" list
        /// </summary>
        /// <param name="customerId">Customer ident</param>
        /// <param name="number">Number of products to load</param>
        /// <returns>"recently viewed products" list</returns>
        Task<IList<Product>> GetRecentlyViewedProducts(string customerId, int number);

        /// <summary>
        /// Adds a product to a recently viewed products list
        /// </summary>
        /// <param name="customerId">Customer ident</param>
        /// <param name="productId">Product identifier</param>
        Task AddProductToRecentlyViewedList(string customerId, string productId);
    }
}
