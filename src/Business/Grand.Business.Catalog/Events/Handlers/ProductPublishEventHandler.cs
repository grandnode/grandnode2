using Grand.Business.Catalog.Events.Models;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class ProductPublishEventHandler : INotificationHandler<ProductPublishEvent>
    {
        private readonly ICacheBase _cacheBase;

        public ProductPublishEventHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(ProductPublishEvent notification, CancellationToken cancellationToken)
        {
            if (notification.Product.ShowOnHomePage)
                await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_SHOWONHOMEPAGE);
        }
    }
}
