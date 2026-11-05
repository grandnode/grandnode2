using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Cms.Events.Cache;

public class RobotsTxtCacheEventHandler :
    INotificationHandler<EntityInserted<RobotsTxt>>,
    INotificationHandler<EntityUpdated<RobotsTxt>>,
    INotificationHandler<EntityDeleted<RobotsTxt>>
{
    private readonly ICacheBase _cacheBase;

    public RobotsTxtCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<RobotsTxt> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);
    }

    public async Task Handle(EntityUpdated<RobotsTxt> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);
    }

    public async Task Handle(EntityDeleted<RobotsTxt> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY);
    }
}
