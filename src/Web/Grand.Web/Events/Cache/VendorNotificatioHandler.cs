using Grand.Infrastructure.Caching;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class VendorNotificatioHandler :
        INotificationHandler<EntityInserted<Vendor>>,
        INotificationHandler<EntityUpdated<Vendor>>,
        INotificationHandler<EntityDeleted<Vendor>>
    {

        private readonly ICacheBase _cacheBase;

        public VendorNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityInserted<Vendor> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
        }
        public async Task Handle(EntityUpdated<Vendor> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
        }
        public async Task Handle(EntityDeleted<Vendor> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.VENDOR_NAVIGATION_PATTERN_KEY);
        }
    }
}