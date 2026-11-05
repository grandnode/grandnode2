using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Shipping.ByWeight.Domain;
using Shipping.ByWeight.Services;

namespace Shipping.ByWeight.Events.Cache;

public class ShippingByWeightCacheEventHandler :
    INotificationHandler<EntityInserted<ShippingByWeightRecord>>,
    INotificationHandler<EntityUpdated<ShippingByWeightRecord>>,
    INotificationHandler<EntityDeleted<ShippingByWeightRecord>>
{
    private readonly ICacheBase _cacheBase;

    public ShippingByWeightCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ShippingByWeightRecord> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingByWeightService.SHIPPINGBYWEIGHT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ShippingByWeightRecord> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingByWeightService.SHIPPINGBYWEIGHT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ShippingByWeightRecord> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(ShippingByWeightService.SHIPPINGBYWEIGHT_PATTERN_KEY);
    }
}
