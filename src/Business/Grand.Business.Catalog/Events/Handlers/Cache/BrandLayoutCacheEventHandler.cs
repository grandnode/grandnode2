using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class BrandLayoutCacheEventHandler :
    INotificationHandler<EntityInserted<BrandLayout>>,
    INotificationHandler<EntityUpdated<BrandLayout>>,
    INotificationHandler<EntityDeleted<BrandLayout>>
{
    private readonly ICacheBase _cacheBase;

    public BrandLayoutCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<BrandLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<BrandLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<BrandLayout> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRAND_LAYOUT_PATTERN_KEY);
    }
}
