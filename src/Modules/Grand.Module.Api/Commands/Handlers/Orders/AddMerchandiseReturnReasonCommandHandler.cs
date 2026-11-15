using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class AddMerchandiseReturnReasonCommandHandler
    : IRequestHandler<AddMerchandiseReturnReasonCommand, MerchandiseReturnReasonDto>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public AddMerchandiseReturnReasonCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<MerchandiseReturnReasonDto> Handle(AddMerchandiseReturnReasonCommand request,
        CancellationToken cancellationToken)
    {
        var reason = request.Model.ToEntity();
        await _merchandiseReturnService.InsertMerchandiseReturnReason(reason);
        return reason.ToModel();
    }
}
