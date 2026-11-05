using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class CheckoutAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<CheckoutAttribute>>,
    INotificationHandler<EntityUpdated<CheckoutAttribute>>,
    INotificationHandler<EntityDeleted<CheckoutAttribute>>
{
    private readonly ICacheBase _cacheBase;

    public CheckoutAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CheckoutAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CheckoutAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CheckoutAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY);
    }
}
