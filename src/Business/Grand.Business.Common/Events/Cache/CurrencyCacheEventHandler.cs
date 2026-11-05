using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class CurrencyCacheEventHandler :
    INotificationHandler<EntityInserted<Currency>>,
    INotificationHandler<EntityUpdated<Currency>>,
    INotificationHandler<EntityDeleted<Currency>>
{
    private readonly ICacheBase _cacheBase;

    public CurrencyCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Currency> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Currency> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Currency> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.CURRENCIES_PATTERN_KEY);
    }
}
