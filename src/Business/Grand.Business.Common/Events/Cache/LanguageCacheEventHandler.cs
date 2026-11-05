using Grand.Domain.Localization;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class LanguageCacheEventHandler :
    INotificationHandler<EntityInserted<Language>>,
    INotificationHandler<EntityUpdated<Language>>,
    INotificationHandler<EntityDeleted<Language>>
{
    private readonly ICacheBase _cacheBase;

    public LanguageCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Language> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Language> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Language> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.LANGUAGES_PATTERN_KEY);
    }
}
