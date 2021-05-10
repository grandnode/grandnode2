using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class ProductNotificatioHandler : INotificationHandler<EntityUpdated<Product>>, INotificationHandler<EntityDeleted<Product>>
    {

        private readonly ICacheBase _cacheBase;

        public ProductNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

       
        public async Task Handle(EntityUpdated<Product> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKeyConst.PRODUCTS_SIMILAR_IDS_PATTERN_KEY, eventMessage.Entity.Id));
            await _cacheBase.RemoveAsync(string.Format(CacheKeyConst.PRODUCTS_RELATED_IDS_PATTERN_KEY, eventMessage.Entity.Id));
        }
        public async Task Handle(EntityDeleted<Product> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKeyConst.PRODUCTS_SIMILAR_IDS_PATTERN_KEY, eventMessage.Entity.Id));
            await _cacheBase.RemoveAsync(string.Format(CacheKeyConst.PRODUCTS_RELATED_IDS_PATTERN_KEY, eventMessage.Entity.Id));
        }
    }
}