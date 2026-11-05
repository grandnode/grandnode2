using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class ProductCollectionCacheEventHandler :
    INotificationHandler<EntityInserted<ProductCollection>>,
    INotificationHandler<EntityUpdated<ProductCollection>>,
    INotificationHandler<EntityDeleted<ProductCollection>>
{
    private readonly ICacheBase _cacheBase;

    public ProductCollectionCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ProductCollection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ProductCollection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ProductCollection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_LISTING_PATTERN_KEY);
    }
}
