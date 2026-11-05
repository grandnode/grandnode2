using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Shipping.ShippingPoint.Domain;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint.Events.Cache;

public class ShippingPointCacheEventHandler :
    INotificationHandler<EntityInserted<ShippingPoints>>,
    INotificationHandler<EntityUpdated<ShippingPoints>>,
    INotificationHandler<EntityDeleted<ShippingPoints>>
{
    private readonly ICacheBase _cacheBase;

    public ShippingPointCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ShippingPoints> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingPointService.PICKUP_POINT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ShippingPoints> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingPointService.PICKUP_POINT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ShippingPoints> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingPointService.PICKUP_POINT_PATTERN_KEY);
    }
}
