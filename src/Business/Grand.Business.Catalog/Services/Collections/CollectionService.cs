using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Security;
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
        IWorkContext workContext,
        IMediator mediator,
        IAclService aclService, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _collectionRepository = collectionRepository;
        _workContext = workContext;
        _mediator = mediator;
        _aclService = aclService;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<Collection> _collectionRepository;
    private readonly IWorkContext _workContext;
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
    /// <param name="storeId">Store ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Collections</returns>
    public virtual async Task<IPagedList<Collection>> GetAllCollections(string collectionName = "",
        string storeId = "",
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var query = from m in _collectionRepository.Table
            select m;

        if (!showHidden)
            query = query.Where(m => m.Published);
        if (!string.IsNullOrWhiteSpace(collectionName))
            query = query.Where(m => m.Name != null && m.Name.ToLower().Contains(collectionName.ToLower()));

        if (!_accessControlConfig.IgnoreAcl ||
            (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations))
        {
            if (!showHidden && !_accessControlConfig.IgnoreAcl)
            {
                //Limited to customer groups rules
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
                //Limited to stores rules
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(storeId)
                    select p;
        }

        query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);
        return await PagedList<Collection>.Create(query, pageIndex, pageSize);
    }


    /// <summary>
    ///     Gets all featured products for collections displayed on the home page
    /// </summary>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Collections</returns>
    public virtual async Task<IList<Collection>> GetAllCollectionFeaturedProductsOnHomePage(bool showHidden = false)
    {
        var query = _collectionRepository.Table.Where(x => x.Published && x.FeaturedProductsOnHomePage)
            .OrderBy(x => x.DisplayOrder);

        var collections = await Task.FromResult(query.ToList());
        if (!showHidden)
            collections = collections
                .Where(c => _aclService.Authorize(c, _workContext.CurrentCustomer) &&
                            _aclService.Authorize(c, _workContext.CurrentStore.Id))
                .ToList();
        return collections;
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

        return await Task.FromResult(query.ToList());
    }

    #endregion
}