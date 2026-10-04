using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetMerchandiseReturnCountQueryHandler : IRequestHandler<GetMerchandiseReturnCountQuery, int>
{
    private readonly IRepository<MerchandiseReturn> _merchandiseReturnRepository;

    public GetMerchandiseReturnCountQueryHandler(IRepository<MerchandiseReturn> merchandiseReturnRepository)
    {
        _merchandiseReturnRepository = merchandiseReturnRepository;
    }

    public async Task<int> Handle(GetMerchandiseReturnCountQuery request, CancellationToken cancellationToken)
    {
        return await _merchandiseReturnRepository.CountAsync(
            _merchandiseReturnRepository.Table.Where(x => x.MerchandiseReturnStatusId == request.RequestStatusId &&
                                                         (string.IsNullOrEmpty(request.StoreId) ||
                                                          x.StoreId == request.StoreId)), cancellationToken);
    }
}