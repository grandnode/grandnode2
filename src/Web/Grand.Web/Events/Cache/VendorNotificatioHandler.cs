using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Web.Events.Cache;

public class VendorNotificatioHandler :
    INotificationHandler<EntityInserted<Domain.Vendors.Vendor>>,
    INotificationHandler<EntityUpdated<Domain.Vendors.Vendor>>,
    INotificationHandler<EntityDeleted<Domain.Vendors.Vendor>>
{
    private readonly ICacheBase _cacheBase;

    public VendorNotificatioHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<Domain.Vendors.Vendor> eventMessage, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<Domain.Vendors.Vendor> eventMessage, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<Domain.Vendors.Vendor> eventMessage, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
    }
}