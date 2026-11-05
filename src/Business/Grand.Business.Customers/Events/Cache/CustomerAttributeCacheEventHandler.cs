using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Customers.Events.Cache;

public class CustomerAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<CustomerAttribute>>,
    INotificationHandler<EntityUpdated<CustomerAttribute>>,
    INotificationHandler<EntityDeleted<CustomerAttribute>>,
    INotificationHandler<EntityInserted<CustomerAttributeValue>>,
    INotificationHandler<EntityUpdated<CustomerAttributeValue>>,
    INotificationHandler<EntityDeleted<CustomerAttributeValue>>
{
    private readonly ICacheBase _cacheBase;

    public CustomerAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<CustomerAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CustomerAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CustomerAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<CustomerAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<CustomerAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<CustomerAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERATTRIBUTEVALUES_PATTERN_KEY);
    }
}
