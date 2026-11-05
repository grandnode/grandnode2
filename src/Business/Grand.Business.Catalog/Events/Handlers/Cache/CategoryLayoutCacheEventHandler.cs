using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class CategoryLayoutCacheEventHandler :
    INotificationHandler<EntityInserted<CategoryLayout>>,
    INotificationHandler<EntityUpdated<CategoryLayout>>,
    INotificationHandler<EntityDeleted<CategoryLayout>>
{
    private readonly ICacheBase _cacheBase;

    public CategoryLayoutCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CategoryLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CategoryLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CategoryLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORY_LAYOUT_PATTERN_KEY);
    }
}
