using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Collections;

/// <summary>
///     Collection service
/// </summary>
public class CollectionService : ICollectionService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public CollectionService(ICacheBase cacheBase,
        IRepository<Collection> collectionRepository,
        IMediator mediator,
        IAclService aclService, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _collectionRepository = collectionRepository;
        _mediator = mediator;
        _aclService = aclService;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<Collection> _collectionRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly IAclService _aclService;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all collections
    /// </summary>
    /// <param name="collectionName">Collection name</param>
    /// <param name="access">Whose view of the collections to return, and the store whose limitations apply</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Collections</returns>
    public virtual async Task<IPagedList<Collection>> GetAllCollections(string collectionName,
        CatalogAccess access,
        int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(access);

        var query = from m in _collectionRepository.Table
            select m;

        if (!access.ShowHidden)
            query = query.Where(m => m.Published);
        if (!string.IsNullOrWhiteSpace(collectionName))
            query = query.Where(m => m.Name != null && m.Name.ToLower().Contains(collectionName.ToLower()));

        if (!_accessControlConfig.IgnoreAcl ||
            (!string.IsNullOrEmpty(access.StoreId) && !_accessControlConfig.IgnoreStoreLimitations))
        {
            if (!access.ShowHidden && !_accessControlConfig.IgnoreAcl)
            {
                //Limited to customer groups rules
                var allowedCustomerGroupsIds = access.Customer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!string.IsNullOrEmpty(access.StoreId) && !_accessControlConfig.IgnoreStoreLimitations)
                //Limited to stores rules
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(access.StoreId)
                    select p;
        }

        query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);
        return await _collectionRepository.PagedAsync(query, pageIndex, pageSize);
    }


    /// <summary>
    ///     Gets all featured products for collections displayed on the home page
    /// </summary>
    /// <param name="access">Whose view of the collections to return, and the store whose limitations apply</param>
    /// <returns>Collections</returns>
    public virtual async Task<IList<Collection>> GetAllCollectionFeaturedProductsOnHomePage(CatalogAccess access)
    {
        ArgumentNullException.ThrowIfNull(access);

        var query = _collectionRepository.Table.Where(x => x.Published && x.FeaturedProductsOnHomePage)
            .OrderBy(x => x.DisplayOrder);

        return (await _collectionRepository.ToListAsync(query))
            .Where(c => (access.ShowHidden || _aclService.Authorize(c, access.Customer)) &&
                        _aclService.Authorize(c, access.StoreId))
            .ToList();
    }


    /// <summary>
    ///     Gets a collection
    /// </summary>
    /// <param name="collectionId">Collection id</param>
    /// <returns>Collection</returns>
    public virtual Task<Collection> GetCollectionById(string collectionId)
    {
        var key = string.Format(CacheKey.COLLECTIONS_BY_ID_KEY, collectionId);
        return _cacheBase.GetAsync(key, () => _collectionRepository.GetByIdAsync(collectionId));
    }

    /// <summary>
    ///     Inserts a collection
    /// </summary>
    /// <param name="collection">Collection</param>
    public virtual async Task InsertCollection(Collection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        await _collectionRepository.InsertAsync(collection);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(collection);
    }

    /// <summary>
    ///     Updates the collection
    /// </summary>
    /// <param name="collection">Collection</param>
    public virtual async Task UpdateCollection(Collection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        await _collectionRepository.UpdateAsync(collection);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(collection);
    }

    /// <summary>
    ///     Deletes a collection
    /// </summary>
    /// <param name="collection">Collection</param>
    public virtual async Task DeleteCollection(Collection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);

        await _collectionRepository.DeleteAsync(collection);

        //event notification
        await _mediator.EntityDeleted(collection);
    }

    /// <summary>
    ///     Gets a discount collection mapping
    /// </summary>
    /// <param name="discountId">Discount id mapping id</param>
    /// <returns>Product collection mapping</returns>
    public virtual async Task<IList<Collection>> GetAllCollectionsByDiscount(string discountId)
    {
        var query = from c in _collectionRepository.Table
            where c.AppliedDiscounts.Any(x => x == discountId)
            select c;

        return await _collectionRepository.ToListAsync(query);
    }

    #endregion
}