using Grand.Business.Catalog.Queries.Models;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Queries.Handlers
{
    public class GetPriceByCustomerProductQueryHandler : IRequestHandler<GetPriceByCustomerProductQuery, double?>
    {
        private readonly ICacheBase _cacheBase;
        private readonly IRepository<CustomerProductPrice> _customerProductPriceRepository;

        public GetPriceByCustomerProductQueryHandler(ICacheBase cacheBase, IRepository<CustomerProductPrice> customerProductPriceRepository)
        {
            _cacheBase = cacheBase;
            _customerProductPriceRepository = customerProductPriceRepository;
        }

        public async Task<double?> Handle(GetPriceByCustomerProductQuery request, CancellationToken cancellationToken)
        {
            var key = string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, request.CustomerId, request.ProductId);
            var productprice = _cacheBase.Get(key, () =>
            {
                var pp = _customerProductPriceRepository.Table
                .Where(x => x.CustomerId == request.CustomerId && x.ProductId == request.ProductId)
                .FirstOrDefault();
                if (pp == null)
                    return (null, false);
                else
                    return (pp, true);
            });

            if (!productprice.Item2)
                return await Task.FromResult(default(double?));
            else
                return await Task.FromResult(productprice.pp.Price);
        }
    }
}
