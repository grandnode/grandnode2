using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class CollectionDeletedEventHandler : INotificationHandler<EntityDeleted<Collection>>
    {
        private readonly IRepository<EntityUrl> _entityUrlRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;

        public CollectionDeletedEventHandler(
            IRepository<EntityUrl> entityUrlRepository,
            IRepository<Product> productRepository,
            ICacheBase cacheBase)
        {
            _entityUrlRepository = entityUrlRepository;
            _productRepository = productRepository;
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityDeleted<Collection> notification, CancellationToken cancellationToken)
        {
            //delete url
            await _entityUrlRepository.DeleteManyAsync(x => x.EntityId == notification.Entity.Id && x.EntityName == "Collection");

            //delete on the product
            await _productRepository.PullFilter(string.Empty, x => x.ProductCollections, z => z.CollectionId, notification.Entity.Id, true);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        }
    }
}