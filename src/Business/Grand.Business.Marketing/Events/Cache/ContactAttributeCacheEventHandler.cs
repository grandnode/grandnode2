using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Marketing.Events.Cache;

public class ContactAttributeCacheEventHandler :
    INotificationHandler<EntityInserted<ContactAttribute>>,
    INotificationHandler<EntityUpdated<ContactAttribute>>,
    INotificationHandler<EntityDeleted<ContactAttribute>>
{
    private readonly ICacheBase _cacheBase;

    public ContactAttributeCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<ContactAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<ContactAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<ContactAttribute> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.CONTACTATTRIBUTEVALUES_PATTERN_KEY);
    }
}
