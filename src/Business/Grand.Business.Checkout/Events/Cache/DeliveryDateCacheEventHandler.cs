using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class DeliveryDateCacheEventHandler :
    INotificationHandler<EntityInserted<DeliveryDate>>,
    INotificationHandler<EntityUpdated<DeliveryDate>>,
    INotificationHandler<EntityDeleted<DeliveryDate>>
{
    private readonly ICacheBase _cacheBase;

    public DeliveryDateCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<DeliveryDate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<DeliveryDate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<DeliveryDate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}
