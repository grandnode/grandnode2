using Grand.Domain.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Events;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Caching;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class CategoryDeletedEventHandler : INotificationHandler<EntityDeleted<Category>>
    {
        private readonly IRepository<EntityUrl> _entityUrlRepository;
        private readonly IRepository<Product> _productRepository;

        private readonly ICacheBase _cacheBase;

        public CategoryDeletedEventHandler(
            IRepository<EntityUrl> entityUrlRepository,
            IRepository<Product> productRepository,
            ICacheBase cacheBase)
        {
            _entityUrlRepository = entityUrlRepository;
            _productRepository = productRepository;

            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityDeleted<Category> notification, CancellationToken cancellationToken)
        {
            //delete url
            var filters = Builders<EntityUrl>.Filter;
            var filter = filters.Eq(x => x.EntityId, notification.Entity.Id);
            filter &= filters.Eq(x => x.EntityName, "Category");
            await _entityUrlRepository.Collection.DeleteManyAsync(filter);

            //delete on the product
            var builder = Builders<Product>.Update;
            var updatefilter = builder.PullFilter(x => x.ProductCategories, y => y.CategoryId == notification.Entity.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        }
    }
}