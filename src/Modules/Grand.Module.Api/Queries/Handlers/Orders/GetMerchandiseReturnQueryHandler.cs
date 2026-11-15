using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Queries.Models.Orders;

namespace Grand.Module.Api.Queries.Handlers.Orders;

public class GetMerchandiseReturnQueryHandler : IRequestHandler<GetMerchandiseReturnQuery, MerchandiseReturn>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public GetMerchandiseReturnQueryHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<MerchandiseReturn> Handle(GetMerchandiseReturnQuery request,
        CancellationToken cancellationToken)
    {
        return await _merchandiseReturnService.GetMerchandiseReturnById(request.Id);
    }
}
