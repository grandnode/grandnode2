using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Queries.Models.Orders;

namespace Grand.Module.Api.Queries.Handlers.Orders;

public class GetMerchandiseReturnsQueryHandler : IRequestHandler<GetMerchandiseReturnsQuery, IPagedList<MerchandiseReturn>>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public GetMerchandiseReturnsQueryHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<IPagedList<MerchandiseReturn>> Handle(GetMerchandiseReturnsQuery request,
        CancellationToken cancellationToken)
    {
        return await _merchandiseReturnService.SearchMerchandiseReturns(
            request.StoreId,
            request.CustomerId,
            request.OrderItemId,
            request.VendorId,
            request.OwnerId,
            request.Status,
            request.PageIndex,
            request.PageSize,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            request.OrderId,
            request.ReturnShippingProvider,
            request.ReasonId,
            request.OpenOnly);
    }
}
