using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Models;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Queries.Handlers
{
    public class GetPersonalizedProductsQueryHandler : IRequestHandler<GetPersonalizedProductsQuery, IList<Product>>
    {

        private readonly IProductService _productService;
        private readonly ICacheBase _cacheBase;
        private readonly IRepository<CustomerProduct> _customerProductRepository;

        public GetPersonalizedProductsQueryHandler(
            IProductService productService,
            ICacheBase cacheBase,
            IRepository<CustomerProduct> customerProductRepository)
        {
            _productService = productService;
            _cacheBase = cacheBase;
            _customerProductRepository = customerProductRepository;
        }

        public async Task<IList<Product>> Handle(GetPersonalizedProductsQuery request, CancellationToken cancellationToken)
        {
            return await _cacheBase.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, request.CustomerId), async () =>
            {
                var query = from cr in _customerProductRepository.Table
                            where cr.CustomerId == request.CustomerId
                            orderby cr.DisplayOrder
                            select cr.ProductId;

                var productIds = query.Take(request.ProductsNumber).ToList();

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
