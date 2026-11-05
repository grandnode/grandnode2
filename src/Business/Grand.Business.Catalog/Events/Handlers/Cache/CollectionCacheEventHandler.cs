using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class CollectionCacheEventHandler :
    INotificationHandler<EntityInserted<Collection>>,
    INotificationHandler<EntityUpdated<Collection>>,
    INotificationHandler<EntityDeleted<Collection>>
{
    private readonly ICacheBase _cacheBase;

    public CollectionCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Collection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Collection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Collection> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTIONS_PATTERN_KEY);
    }
}
