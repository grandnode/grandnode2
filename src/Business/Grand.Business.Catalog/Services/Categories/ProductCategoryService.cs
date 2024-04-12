using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Categories;

public class ProductCategoryService : IProductCategoryService
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;
    private readonly IRepository<Product> _productRepository;
    private readonly IWorkContext _workContext;

    public ProductCategoryService(
        IRepository<Product> productRepository,
        ICacheBase cacheBase,
        IWorkContext workContext,
        IMediator mediator, AccessControlConfig accessControlConfig)
    {
        _productRepository = productRepository;
        _cacheBase = cacheBase;
        _workContext = workContext;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    /// <summary>
    ///     Gets product category mapping collection
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Product a category mapping collection</returns>
    public virtual async Task<IPagedList<ProductsCategory>> GetProductCategoriesByCategoryId(string categoryId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        if (string.IsNullOrEmpty(categoryId))
            return new PagedList<ProductsCategory>(new List<ProductsCategory>(), pageIndex, pageSize);

        var key = string.Format(CacheKey.PRODUCTCATEGORIES_ALLBYCATEGORYID_KEY, showHidden, categoryId, pageIndex,
            pageSize, _workContext.CurrentCustomer.Id, _workContext.CurrentStore.Id);
        return await _cacheBase.GetAsync(key, () =>
        {
            var query = _productRepository.Table.Where(x => x.ProductCategories.Any(y => y.CategoryId == categoryId));

            if (!showHidden && (!_accessControlConfig.IgnoreAcl || !_accessControlConfig.IgnoreStoreLimitations))
            {
                if (!_accessControlConfig.IgnoreAcl)
                {
                    //Limited to customer groups
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
                }

                if (!_accessControlConfig.IgnoreStoreLimitations)
                {
                    //Limited to stores
                    var currentStoreId = _workContext.CurrentStore.Id;
                    query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(currentStoreId)
                        select p;
                }
            }

            var queryProductCategories = from prod in query
                from pc in prod.ProductCategories
                select new ProductsCategory {
                    CategoryId = pc.CategoryId,
                    DisplayOrder = pc.DisplayOrder,
                    Id = pc.Id,
                    ProductId = prod.Id,
                    IsFeaturedProduct = pc.IsFeaturedProduct
                };

            queryProductCategories = from pm in queryProductCategories
                where pm.CategoryId == categoryId
                orderby pm.DisplayOrder
                select pm;

            return Task.FromResult(new PagedList<ProductsCategory>(queryProductCategories, pageIndex, pageSize));
        });
    }


    /// <summary>
    ///     Inserts a product category
    /// </summary>
    /// <param name="productCategory">>Product category mapping</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task InsertProductCategory(ProductCategory productCategory, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCategory);

        await _productRepository.AddToSet(productId, x => x.ProductCategories, productCategory);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityInserted(productCategory);
    }

    /// <summary>
    ///     Updates the associated product category
    /// </summary>
    /// <param name="productCategory">>Product category mapping</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task UpdateProductCategory(ProductCategory productCategory, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCategory);

        await _productRepository.UpdateToSet(productId, x => x.ProductCategories, z => z.Id, productCategory.Id,
            productCategory);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityUpdated(productCategory);
    }

    /// <summary>
    ///     Deletes a associated product category
    /// </summary>
    /// <param name="productCategory">Product category</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task DeleteProductCategory(ProductCategory productCategory, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCategory);

        await _productRepository.PullFilter(productId, x => x.ProductCategories, z => z.Id, productCategory.Id);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityDeleted(productCategory);
    }
}