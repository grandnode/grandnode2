using Grand.Domain.Catalog;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

/// <summary>
///     Recently viewed products service
/// </summary>
public interface IRecentlyViewedProductsService
{
    /// <summary>
    ///     Gets a "recently viewed products" list
    /// </summary>
    /// <param name="customer">Customer whose list is loaded and whose groups limit it</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="number">Number of products to load</param>
    /// <returns>"recently viewed products" list</returns>
    Task<IList<Product>> GetRecentlyViewedProducts(Customer customer, string storeId, int number);

    /// <summary>
    ///     Adds a product to a recently viewed products list
    /// </summary>
    /// <param name="customerId">Customer ident</param>
    /// <param name="productId">Product identifier</param>
    Task AddProductToRecentlyViewedList(string customerId, string productId);
}