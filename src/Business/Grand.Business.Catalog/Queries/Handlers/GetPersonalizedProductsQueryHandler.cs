using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetPersonalizedProductsQueryHandler : IRequestHandler<GetPersonalizedProductsQuery, IList<Product>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<CustomerProduct> _customerProductRepository;

    private readonly IProductService _productService;

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
        return await _cacheBase.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, request.CustomerId),
            async () =>
            {
                var query = from cr in _customerProductRepository.Table
                    where cr.CustomerId == request.CustomerId
                    orderby cr.DisplayOrder
                    select cr.ProductId;

                var productIds = query.Take(request.ProductsNumber).ToList();

                var ids = await _productService.GetProductsByIds(productIds.Distinct().ToArray());

                return ids.Where(product => product.Published).ToList();
            });
    }
}