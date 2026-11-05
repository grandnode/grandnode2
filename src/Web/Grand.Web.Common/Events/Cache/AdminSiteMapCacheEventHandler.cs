using Grand.Domain.Admin;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Web.Common.Events.Cache;

public class AdminSiteMapCacheEventHandler :
    INotificationHandler<EntityInserted<AdminSiteMap>>,
    INotificationHandler<EntityUpdated<AdminSiteMap>>,
    INotificationHandler<EntityDeleted<AdminSiteMap>>
{
    private readonly ICacheBase _cacheBase;

    public AdminSiteMapCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<AdminSiteMap> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<AdminSiteMap> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<AdminSiteMap> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ADMIN_SITEMAP_PATTERN_KEY);
    }
}
