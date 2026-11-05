using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Customers.Events.Cache;

public class SalesEmployeeCacheEventHandler :
    INotificationHandler<EntityInserted<SalesEmployee>>,
    INotificationHandler<EntityUpdated<SalesEmployee>>,
    INotificationHandler<EntityDeleted<SalesEmployee>>
{
    private readonly ICacheBase _cacheBase;

    public SalesEmployeeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<SalesEmployee> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<SalesEmployee> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<SalesEmployee> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);
    }
}
