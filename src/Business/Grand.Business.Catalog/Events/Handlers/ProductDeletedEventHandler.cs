using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Events;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Seo;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class ProductDeletedEventHandler : INotificationHandler<EntityDeleted<Product>>
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<CustomerGroupProduct> _customerGroupProductRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<EntityUrl> _entityUrlRepository;
        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IRepository<ProductReview> _productReviewRepository;
        private readonly IRepository<ProductDeleted> _productDeletedRepository;
        private readonly IProductTagService _productTagService;

        public ProductDeletedEventHandler(
            IRepository<Product> productRepository,
            IRepository<CustomerGroupProduct> customerGroupProductRepository,
            IRepository<Customer> customerRepository,
            IRepository<EntityUrl> entityUrlRepository,
            IRepository<ProductTag> productTagRepository,
            IRepository<ProductReview> productReviewRepository,
            IRepository<ProductDeleted> productDeletedRepository,
            IProductTagService productTagService)
        {
            _productRepository = productRepository;
            _customerGroupProductRepository = customerGroupProductRepository;
            _customerRepository = customerRepository;
            _entityUrlRepository = entityUrlRepository;
            _productTagRepository = productTagRepository;
            _productReviewRepository = productReviewRepository;
            _productDeletedRepository = productDeletedRepository;
            _productTagService = productTagService;
        }

        public async Task Handle(EntityDeleted<Product> notification, CancellationToken cancellationToken)
        {
            //delete related product
            var builderRelated = Builders<Product>.Update;
            var updatefilterRelated = builderRelated.PullFilter(x => x.RelatedProducts, y => y.ProductId2 == notification.Entity.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilterRelated);

            //delete similar product
            var builderSimilar = Builders<Product>.Update;
            var updatefilterSimilar = builderSimilar.PullFilter(x => x.SimilarProducts, y => y.ProductId2 == notification.Entity.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilterSimilar);

            //delete cross sales product
            var builderCross = Builders<Product>.Update;
            var updatefilterCross = builderCross.Pull(x => x.CrossSellProduct, notification.Entity.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilterCross);


            //delete review
            var filtersProductReview = Builders<ProductReview>.Filter;
            var filterProdReview = filtersProductReview.Eq(x => x.ProductId, notification.Entity.Id);
            await _productReviewRepository.Collection.DeleteManyAsync(filterProdReview);

            //delete from shopping cart
            var builder = Builders<Customer>.Update;
            var updatefilter = builder.PullFilter(x => x.ShoppingCartItems, y => y.ProductId == notification.Entity.Id);
            await _customerRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);

            //delete customer group product
            var filtersCrp = Builders<CustomerGroupProduct>.Filter;
            var filterCrp = filtersCrp.Eq(x => x.ProductId, notification.Entity.Id);
            await _customerGroupProductRepository.Collection.DeleteManyAsync(filterCrp);

            //delete url
            var filters = Builders<EntityUrl>.Filter;
            var filter = filters.Eq(x => x.EntityId, notification.Entity.Id);
            filter = filter & filters.Eq(x => x.EntityName, "Product");
            await _entityUrlRepository.Collection.DeleteManyAsync(filter);

            //delete product tags
            var existingProductTags = _productTagRepository.Table.Where(x => notification.Entity.ProductTags.ToList().Contains(x.Name)).ToList();
            foreach (var tag in existingProductTags)
            {
                await _productTagService.DetachProductTag(tag, notification.Entity.Id);
            }

            //insert to deleted products
            var productDeleted = JsonConvert.DeserializeObject<ProductDeleted>(JsonConvert.SerializeObject(notification.Entity));
            productDeleted.DeletedOnUtc = DateTime.UtcNow;
            await _productDeletedRepository.InsertAsync(productDeleted);

        }
    }
}
