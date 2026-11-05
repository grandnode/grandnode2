using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Cms.Events.Cache;

public class PageLayoutCacheEventHandler :
    INotificationHandler<EntityInserted<PageLayout>>,
    INotificationHandler<EntityUpdated<PageLayout>>,
    INotificationHandler<EntityDeleted<PageLayout>>
{
    private readonly ICacheBase _cacheBase;

    public PageLayoutCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<PageLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<PageLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<PageLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);
    }
}
