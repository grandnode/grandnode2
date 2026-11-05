using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Cache;

public class MerchandiseReturnCacheEventHandler :
    INotificationHandler<EntityInserted<MerchandiseReturnAction>>,
    INotificationHandler<EntityUpdated<MerchandiseReturnAction>>,
    INotificationHandler<EntityDeleted<MerchandiseReturnAction>>,
    INotificationHandler<EntityInserted<MerchandiseReturnReason>>,
    INotificationHandler<EntityUpdated<MerchandiseReturnReason>>,
    INotificationHandler<EntityDeleted<MerchandiseReturnReason>>
{
    private readonly ICacheBase _cacheBase;

    public MerchandiseReturnCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<MerchandiseReturnAction> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MerchandiseReturnAction> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MerchandiseReturnAction> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<MerchandiseReturnReason> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MerchandiseReturnReason> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MerchandiseReturnReason> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY);
    }
}
