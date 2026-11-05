using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class OrderStatusCacheEventHandler :
    INotificationHandler<EntityInserted<OrderStatus>>,
    INotificationHandler<EntityUpdated<OrderStatus>>,
    INotificationHandler<EntityDeleted<OrderStatus>>
{
    private readonly ICacheBase _cacheBase;

    public OrderStatusCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<OrderStatus> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);
    }

    public async Task Handle(EntityUpdated<OrderStatus> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);
    }

    public async Task Handle(EntityDeleted<OrderStatus> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);
    }
}
