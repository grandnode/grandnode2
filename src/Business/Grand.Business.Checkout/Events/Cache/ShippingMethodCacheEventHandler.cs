using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class ShippingMethodCacheEventHandler :
    INotificationHandler<EntityInserted<ShippingMethod>>,
    INotificationHandler<EntityUpdated<ShippingMethod>>,
    INotificationHandler<EntityDeleted<ShippingMethod>>
{
    private readonly ICacheBase _cacheBase;

    public ShippingMethodCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ShippingMethod> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ShippingMethod> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ShippingMethod> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY);
    }
}
