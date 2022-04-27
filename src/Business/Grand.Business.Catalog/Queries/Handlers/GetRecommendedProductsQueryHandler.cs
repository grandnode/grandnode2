using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers
{
    public class GetRecommendedProductsQueryHandler : IRequestHandler<GetRecommendedProductsQuery, IList<Product>>
    {

        private readonly IProductService _productService;
        private readonly ICacheBase _cacheBase;
        private readonly IRepository<CustomerGroupProduct> _customerGroupProductRepository;

        public GetRecommendedProductsQueryHandler(
            IProductService productService,
            ICacheBase cacheBase,
            IRepository<CustomerGroupProduct> customerGroupProductRepository)
        {
            _productService = productService;
            _cacheBase = cacheBase;
            _customerGroupProductRepository = customerGroupProductRepository;
        }

        public async Task<IList<Product>> Handle(GetRecommendedProductsQuery request, CancellationToken cancellationToken)
        {
            return await _cacheBase.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_GROUP, string.Join(",", request.CustomerGroupIds), request.StoreId), async () =>
            {
                var query = from cr in _customerGroupProductRepository.Table
                            where request.CustomerGroupIds.Contains(cr.CustomerGroupId)
                            orderby cr.DisplayOrder
                            select cr.ProductId;

                var productIds = query.ToList();

                var products = new List<Product>();
                var ids = await _productService.GetProductsByIds(productIds.Distinct().ToArray());
                foreach (var product in ids)
                    if (product.Published)
                        products.Add(product);

                return products;
            });

        }
    }
}
