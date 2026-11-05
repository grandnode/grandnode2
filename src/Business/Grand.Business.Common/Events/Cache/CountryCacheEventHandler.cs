using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class CountryCacheEventHandler :
    INotificationHandler<EntityInserted<Country>>,
    INotificationHandler<EntityUpdated<Country>>,
    INotificationHandler<EntityDeleted<Country>>
{
    private readonly ICacheBase _cacheBase;

    public CountryCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Country> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Country> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Country> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.COUNTRIES_PATTERN_KEY);
    }
}
