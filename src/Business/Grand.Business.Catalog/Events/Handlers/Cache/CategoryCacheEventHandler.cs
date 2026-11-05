using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class CategoryCacheEventHandler :
    INotificationHandler<EntityInserted<Category>>,
    INotificationHandler<EntityUpdated<Category>>,
    INotificationHandler<EntityDeleted<Category>>
{
    private readonly ICacheBase _cacheBase;

    public CategoryCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Category> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Category> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Category> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CATEGORIES_PATTERN_KEY);
    }
}
