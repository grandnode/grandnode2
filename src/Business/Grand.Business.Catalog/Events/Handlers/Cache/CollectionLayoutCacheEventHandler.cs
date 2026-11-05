using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class CollectionLayoutCacheEventHandler :
    INotificationHandler<EntityInserted<CollectionLayout>>,
    INotificationHandler<EntityUpdated<CollectionLayout>>,
    INotificationHandler<EntityDeleted<CollectionLayout>>
{
    private readonly ICacheBase _cacheBase;

    public CollectionLayoutCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CollectionLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CollectionLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CollectionLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COLLECTION_LAYOUT_PATTERN_KEY);
    }
}
