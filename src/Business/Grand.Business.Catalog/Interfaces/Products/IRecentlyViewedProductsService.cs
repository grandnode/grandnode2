using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Interfaces.Products
{
    /// <summary>
    /// Recently viewed products service
    /// </summary>
    public partial interface IRecentlyViewedProductsService
    {
        /// <summary>
        /// Gets a "recently viewed products" list
        /// </summary>
        /// <param name="number">Number of products to load</param>
        /// <returns>"recently viewed products" list</returns>
        Task<IList<Product>> GetRecentlyViewedProducts(string customerId, int number);

        /// <summary>
        /// Adds a product to a recently viewed products list
        /// </summary>
        /// <param name="productId">Product identifier</param>
        Task AddProductToRecentlyViewedList(string customerId, string productId);
    }
}
