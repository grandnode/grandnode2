using Grand.Domain.Permissions;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Common.Events.Cache;

public class PermissionCacheEventHandler :
    INotificationHandler<EntityInserted<Permission>>,
    INotificationHandler<EntityUpdated<Permission>>,
    INotificationHandler<EntityDeleted<Permission>>,
    INotificationHandler<EntityInserted<PermissionAction>>,
    INotificationHandler<EntityDeleted<PermissionAction>>
{
    private readonly ICacheBase _cacheBase;

    public PermissionCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<Permission> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Permission> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<Permission> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<PermissionAction> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<PermissionAction> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
    }
}
