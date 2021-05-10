using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using MongoDB.Driver.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class GetMerchandiseReturnQueryHandler : IRequestHandler<GetMerchandiseReturnQuery, IMongoQueryable<MerchandiseReturn>>
    {
        private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;

        public GetMerchandiseReturnQueryHandler(IRepository<MerchandiseReturn> merchandiseReturnRepository)
        {
            _merchandiseReturnRepository = merchandiseReturnRepository;
        }

        public Task<IMongoQueryable<MerchandiseReturn>> Handle(GetMerchandiseReturnQuery request, CancellationToken cancellationToken)
        {
            var query = _merchandiseReturnRepository.Table;
            if (!string.IsNullOrEmpty(request.StoreId))
                query = query.Where(rr => request.StoreId == rr.StoreId);

            if (!string.IsNullOrEmpty(request.CustomerId))
                query = query.Where(rr => request.CustomerId == rr.CustomerId);

            if (!string.IsNullOrEmpty(request.VendorId))
                query = query.Where(rr => request.VendorId == rr.VendorId);

            if (!string.IsNullOrEmpty(request.OwnerId))
                query = query.Where(rr => request.OwnerId == rr.OwnerId);

            if (request.Rs.HasValue)
            {
                var returnStatusId = (int)request.Rs.Value;
                query = query.Where(rr => rr.MerchandiseReturnStatusId == returnStatusId);
            }
            if (!string.IsNullOrEmpty(request.OrderItemId))
                query = query.Where(rr => rr.MerchandiseReturnItems.Any(x => x.OrderItemId == request.OrderItemId));

            if (request.CreatedFromUtc.HasValue)
                query = query.Where(rr => request.CreatedFromUtc.Value <= rr.CreatedOnUtc);

            if (request.CreatedToUtc.HasValue)
                query = query.Where(rr => request.CreatedToUtc.Value >= rr.CreatedOnUtc);

            query = query.OrderByDescending(rr => rr.CreatedOnUtc).ThenByDescending(rr => rr.Id);

            return Task.FromResult(query);
        }
    }
}
