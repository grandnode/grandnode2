using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class BrandCacheEventHandler :
    INotificationHandler<EntityInserted<Brand>>,
    INotificationHandler<EntityUpdated<Brand>>,
    INotificationHandler<EntityDeleted<Brand>>
{
    private readonly ICacheBase _cacheBase;

    public BrandCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Brand> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Brand> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Brand> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);
    }
}
