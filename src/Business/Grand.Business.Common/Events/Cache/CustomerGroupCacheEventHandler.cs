using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class CustomerGroupCacheEventHandler :
    INotificationHandler<EntityInserted<CustomerGroup>>,
    INotificationHandler<EntityUpdated<CustomerGroup>>,
    INotificationHandler<EntityDeleted<CustomerGroup>>
{
    private readonly ICacheBase _cacheBase;

    public CustomerGroupCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CustomerGroup> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CustomerGroup> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CustomerGroup> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);
    }
}
