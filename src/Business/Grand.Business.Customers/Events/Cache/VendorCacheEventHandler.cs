using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Customers.Events.Cache;

public class VendorCacheEventHandler :
    INotificationHandler<EntityUpdated<Vendor>>
{
    private readonly ICacheBase _cacheBase;

    public VendorCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityUpdated<Vendor> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveAsync(string.Format(CacheKey.VENDOR_BY_ID_KEY, notification.Entity.Id));
    }
}
