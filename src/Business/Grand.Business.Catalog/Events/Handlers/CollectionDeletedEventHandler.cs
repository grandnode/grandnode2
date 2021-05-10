using Grand.Domain.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Events;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MongoDB.Bson;

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
            var filters = Builders<EntityUrl>.Filter;
            var filter = filters.Eq(x => x.EntityId, notification.Entity.Id);
            filter = filter & filters.Eq(x => x.EntityName, "Collection");
            await _entityUrlRepository.Collection.DeleteManyAsync(filter);

            //delete on the product
            var builder = Builders<Product>.Update;
            var updatefilter = builder.PullFilter(x => x.ProductCollections, y => y.CollectionId == notification.Entity.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        }
    }
}