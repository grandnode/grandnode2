using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class WarehouseCacheEventHandler :
    INotificationHandler<EntityInserted<Warehouse>>,
    INotificationHandler<EntityUpdated<Warehouse>>,
    INotificationHandler<EntityDeleted<Warehouse>>
{
    private readonly ICacheBase _cacheBase;

    public WarehouseCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Warehouse> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Warehouse> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Warehouse> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}
