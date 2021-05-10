using Grand.Infrastructure.Caching;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class OrderNotificatioHandler :
        INotificationHandler<EntityInserted<Order>>,
        INotificationHandler<EntityUpdated<Order>>,
        INotificationHandler<EntityDeleted<Order>>
    {

        private readonly ICacheBase _cacheBase;

        public OrderNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityInserted<Order> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
        }
        public async Task Handle(EntityUpdated<Order> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
        }
        public async Task Handle(EntityDeleted<Order> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
        }
    }
}