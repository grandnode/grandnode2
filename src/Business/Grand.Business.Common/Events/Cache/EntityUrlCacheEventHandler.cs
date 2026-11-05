using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class EntityUrlCacheEventHandler :
    INotificationHandler<EntityInserted<EntityUrl>>,
    INotificationHandler<EntityUpdated<EntityUrl>>,
    INotificationHandler<EntityDeleted<EntityUrl>>
{
    private readonly ICacheBase _cacheBase;

    public EntityUrlCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<EntityUrl> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<EntityUrl> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<EntityUrl> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
    }
}
