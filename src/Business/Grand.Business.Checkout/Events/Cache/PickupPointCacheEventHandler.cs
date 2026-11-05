using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class PickupPointCacheEventHandler :
    INotificationHandler<EntityInserted<PickupPoint>>,
    INotificationHandler<EntityUpdated<PickupPoint>>,
    INotificationHandler<EntityDeleted<PickupPoint>>
{
    private readonly ICacheBase _cacheBase;

    public PickupPointCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<PickupPoint> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<PickupPoint> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<PickupPoint> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);
    }
}
