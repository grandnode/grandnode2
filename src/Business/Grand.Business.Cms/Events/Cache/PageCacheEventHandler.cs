using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Cms.Events.Cache;

public class PageCacheEventHandler :
    INotificationHandler<EntityInserted<Page>>,
    INotificationHandler<EntityUpdated<Page>>,
    INotificationHandler<EntityDeleted<Page>>
{
    private readonly ICacheBase _cacheBase;

    public PageCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Page> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Page> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Page> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
    }
}
