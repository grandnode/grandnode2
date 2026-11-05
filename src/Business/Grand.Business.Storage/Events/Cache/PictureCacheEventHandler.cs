using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Storage.Events.Cache;

public class PictureCacheEventHandler :
    INotificationHandler<EntityUpdated<Picture>>
{
    private readonly ICacheBase _cacheBase;

    public PictureCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityUpdated<Picture> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PICTURE_BY_ID, notification.Entity.Id));
    }
}
