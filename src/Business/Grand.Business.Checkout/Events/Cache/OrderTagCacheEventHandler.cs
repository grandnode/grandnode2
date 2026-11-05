using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class OrderTagCacheEventHandler :
    INotificationHandler<EntityInserted<OrderTag>>,
    INotificationHandler<EntityUpdated<OrderTag>>,
    INotificationHandler<EntityDeleted<OrderTag>>
{
    private readonly ICacheBase _cacheBase;

    public OrderTagCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<OrderTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<OrderTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<OrderTag> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ORDERS_PATTERN_KEY);
    }
}
