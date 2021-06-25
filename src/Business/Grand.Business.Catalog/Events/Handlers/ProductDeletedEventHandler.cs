using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Events;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Seo;
using MediatR;
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
            await _productRepository.PullFilter(string.Empty, x => x.RelatedProducts, z => z.ProductId2, notification.Entity.Id, true);

            //delete similar product
            await _productRepository.PullFilter(string.Empty, x => x.SimilarProducts, z => z.ProductId2, notification.Entity.Id, true);

            //delete cross sales product
            await _productRepository.Pull(string.Empty, x => x.CrossSellProduct, notification.Entity.Id, true);

            //delete recomended product
            await _productRepository.Pull(string.Empty, x => x.RecommendedProduct, notification.Entity.Id, true);

            //delete review
            await _productReviewRepository.DeleteManyAsync(x=>x.ProductId == notification.Entity.Id);

            //delete from shopping cart
            await _customerRepository.PullFilter(string.Empty, x => x.ShoppingCartItems, z => z.ProductId, notification.Entity.Id, true);

            //delete customer group product
            await _customerGroupProductRepository.DeleteManyAsync(x => x.ProductId == notification.Entity.Id);

            //delete url
            await _entityUrlRepository.DeleteManyAsync(x => x.EntityId == notification.Entity.Id && x.EntityName == "Product");

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
