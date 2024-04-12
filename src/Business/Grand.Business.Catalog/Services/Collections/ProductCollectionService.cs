using Grand.Business.Core.Interfaces.Catalog.Collections;
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

namespace Grand.Business.Catalog.Services.Collections;

public class ProductCollectionService : IProductCollectionService
{
    #region Ctor

    public ProductCollectionService(ICacheBase cacheBase,
        IRepository<Product> productRepository,
        IWorkContext workContext,
        IMediator mediator, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _productRepository = productRepository;
        _workContext = workContext;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    /// <summary>
    ///     Gets product collection by collection id
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Product collection collection</returns>
    public virtual async Task<IPagedList<ProductsCollection>> GetProductCollectionsByCollectionId(string collectionId,
        string storeId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var key = string.Format(CacheKey.PRODUCTCOLLECTIONS_ALLBYCOLLECTIONID_KEY, showHidden, collectionId, pageIndex,
            pageSize, _workContext.CurrentCustomer.Id, storeId);
        return await _cacheBase.GetAsync(key, () =>
        {
            var query = _productRepository.Table.Where(x =>
                x.ProductCollections.Any(y => y.CollectionId == collectionId));

            if (!showHidden && (!_accessControlConfig.IgnoreAcl || !_accessControlConfig.IgnoreStoreLimitations))
            {
                if (!_accessControlConfig.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                    query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
                }

                if (!_accessControlConfig.IgnoreStoreLimitations && !string.IsNullOrEmpty(storeId))
                    //Store acl
                    query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        select p;
            }

            var queryProductCollection = from prod in query
                from pm in prod.ProductCollections
                select new ProductsCollection {
                    Id = pm.Id,
                    ProductId = prod.Id,
                    DisplayOrder = pm.DisplayOrder,
                    IsFeaturedProduct = pm.IsFeaturedProduct,
                    CollectionId = pm.CollectionId
                };

            queryProductCollection = from pm in queryProductCollection
                where pm.CollectionId == collectionId
                orderby pm.DisplayOrder
                select pm;

            return Task.FromResult(new PagedList<ProductsCollection>(queryProductCollection, pageIndex, pageSize));
        });
    }

    /// <summary>
    ///     Inserts a product collection mapping
    /// </summary>
    /// <param name="productCollection">Product collection mapping</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task InsertProductCollection(ProductCollection productCollection, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCollection);

        await _productRepository.AddToSet(productId, x => x.ProductCollections, productCollection);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityInserted(productCollection);
    }

    /// <summary>
    ///     Updates the product collection mapping
    /// </summary>
    /// <param name="productCollection">Product collection mapping</param>
    /// <param name="productId">Product id</param>
    public virtual async Task UpdateProductCollection(ProductCollection productCollection, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCollection);

        await _productRepository.UpdateToSet(productId, x => x.ProductCollections, z => z.Id, productCollection.Id,
            productCollection);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityUpdated(productCollection);
    }

    /// <summary>
    ///     Deletes a product collection mapping
    /// </summary>
    /// <param name="productCollection">Product collection mapping</param>
    /// <param name="productId">Product id</param>
    public virtual async Task DeleteProductCollection(ProductCollection productCollection, string productId)
    {
        ArgumentNullException.ThrowIfNull(productCollection);

        await _productRepository.PullFilter(productId, x => x.ProductCollections, z => z.Id, productCollection.Id);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        //event notification
        await _mediator.EntityDeleted(productCollection);
    }

    #region Fields

    private readonly IRepository<Product> _productRepository;
    private readonly IWorkContext _workContext;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion
}