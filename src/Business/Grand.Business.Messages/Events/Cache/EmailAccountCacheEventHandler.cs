using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Messages.Events.Cache;

public class EmailAccountCacheEventHandler :
    INotificationHandler<EntityInserted<EmailAccount>>,
    INotificationHandler<EntityUpdated<EmailAccount>>,
    INotificationHandler<EntityDeleted<EmailAccount>>
{
    private readonly ICacheBase _cacheBase;

    public EmailAccountCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<EmailAccount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<EmailAccount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<EmailAccount> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.EMAILACCOUNT_PATTERN_KEY);
    }
}
