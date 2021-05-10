using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class GetMerchandiseReturnCountQueryHandler : IRequestHandler<GetMerchandiseReturnCountQuery, int>
    {
        private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;

        public GetMerchandiseReturnCountQueryHandler(IRepository<MerchandiseReturn> merchandiseReturnRepository)
        {
            _merchandiseReturnRepository = merchandiseReturnRepository;
        }

        public async Task<int> Handle(GetMerchandiseReturnCountQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(
                _merchandiseReturnRepository.Table.Where(x => x.MerchandiseReturnStatusId == request.RequestStatusId &&
                (string.IsNullOrEmpty(request.StoreId) || x.StoreId == request.StoreId)).Count());
        }
    }
}
