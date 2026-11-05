using Grand.Domain.Discounts;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers.Cache;

public class DiscountCacheEventHandler :
    INotificationHandler<EntityInserted<Discount>>,
    INotificationHandler<EntityUpdated<Discount>>,
    INotificationHandler<EntityDeleted<Discount>>,
    INotificationHandler<EntityInserted<DiscountCoupon>>,
    INotificationHandler<EntityDeleted<DiscountCoupon>>,
    INotificationHandler<EntityInserted<DiscountUsageHistory>>,
    INotificationHandler<EntityUpdated<DiscountUsageHistory>>,
    INotificationHandler<EntityDeleted<DiscountUsageHistory>>
{
    private readonly ICacheBase _cacheBase;

    public DiscountCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Discount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Discount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Discount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<DiscountCoupon> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<DiscountCoupon> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<DiscountUsageHistory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<DiscountUsageHistory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<DiscountUsageHistory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }
}
