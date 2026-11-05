using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class AddressAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<AddressAttribute>>,
    INotificationHandler<EntityUpdated<AddressAttribute>>,
    INotificationHandler<EntityDeleted<AddressAttribute>>,
    INotificationHandler<EntityInserted<AddressAttributeValue>>,
    INotificationHandler<EntityUpdated<AddressAttributeValue>>,
    INotificationHandler<EntityDeleted<AddressAttributeValue>>
{
    private readonly ICacheBase _cacheBase;

    public AddressAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<AddressAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<AddressAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<AddressAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<AddressAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<AddressAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<AddressAttributeValue> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.ADDRESSATTRIBUTEVALUES_PATTERN_KEY);
    }
}
