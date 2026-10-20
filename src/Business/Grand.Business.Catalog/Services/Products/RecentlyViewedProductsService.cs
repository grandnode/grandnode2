using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Recently viewed products service
/// </summary>
public class RecentlyViewedProductsService : IRecentlyViewedProductsService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="productService">Product service</param>
    /// <param name="catalogSettings">Catalog settings</param>
    /// <param name="recentlyViewedProducts">Collection recentlyViewedProducts</param>
    public RecentlyViewedProductsService(
        IProductService productService,
        CatalogSettings catalogSettings,
        IRepository<RecentlyViewedProduct> recentlyViewedProducts)
    {
        _productService = productService;
        _catalogSettings = catalogSettings;
        _recentlyViewedProducts = recentlyViewedProducts;
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly CatalogSettings _catalogSettings;
    private readonly IRepository<RecentlyViewedProduct> _recentlyViewedProducts;

    #endregion

    #region Utilities

    /// <summary>
    ///     Gets a "recently viewed products" identifier list
    /// </summary>
    /// <param name="customerId">Customer ident</param>
    /// <param name="number">Number of products to load</param>
    /// <returns>"recently viewed products" list</returns>
    /// <remarks>
    ///     Not cached: every product view would have to invalidate the entry on every instance, while the read is
    ///     one indexed query of a few ids.
    /// </remarks>
    protected async Task<IList<string>> GetRecentlyViewedProductsIds(string customerId, int number)
    {
        var query = from p in _recentlyViewedProducts.Table
            where p.CustomerId == customerId
            orderby p.CreatedOnUtc descending, p.Id descending
            select p.ProductId;
        return await _recentlyViewedProducts.ToListAsync(query.Take(number));
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a "recently viewed products" list
    /// </summary>
    /// <param name="customer">Customer whose list is loaded and whose groups limit it</param>
    /// <param name="storeId">Store whose limitations apply</param>
    /// <param name="number">Number of products to load</param>
    /// <returns>"recently viewed products" list</returns>
    public virtual async Task<IList<Product>> GetRecentlyViewedProducts(Customer customer, string storeId, int number)
    {
        var productIds = await GetRecentlyViewedProductsIds(customer.Id, number);
        return (await _productService.GetProductsByIds(productIds.ToArray(), CatalogAccess.For(customer, storeId)))
            .Where(product => product.Published)
            .ToList();
    }

    /// <summary>
    ///     Adds a product to a recently viewed products list
    /// </summary>
    /// <param name="customerId">Customer ident</param>
    /// <param name="productId">Product identifier</param>
    public virtual async Task AddProductToRecentlyViewedList(string customerId, string productId)
    {
        if (!_catalogSettings.RecentlyViewedProductsEnabled)
            return;

        //a product viewed again is inserted anew, so it moves to the top of the list
        await _recentlyViewedProducts.DeleteManyAsync(x => x.CustomerId == customerId && x.ProductId == productId);
        await _recentlyViewedProducts.InsertAsync(new RecentlyViewedProduct
            { CustomerId = customerId, ProductId = productId });

        var maxProducts = _catalogSettings.RecentlyViewedProductsNumber;
        if (maxProducts <= 0)
            maxProducts = 10;

        //only the ids of the rows beyond the limit leave the database
        var query = from p in _recentlyViewedProducts.Table
            where p.CustomerId == customerId
            orderby p.CreatedOnUtc descending, p.Id descending
            select p.Id;
        var staleIds = await _recentlyViewedProducts.ToListAsync(query.Skip(maxProducts));
        if (staleIds.Any())
            await _recentlyViewedProducts.DeleteManyAsync(x => staleIds.Contains(x.Id));
    }

    #endregion
}