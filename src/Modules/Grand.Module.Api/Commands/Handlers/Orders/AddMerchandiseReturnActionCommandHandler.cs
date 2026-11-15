using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class AddMerchandiseReturnActionCommandHandler
    : IRequestHandler<AddMerchandiseReturnActionCommand, MerchandiseReturnActionDto>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public AddMerchandiseReturnActionCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<MerchandiseReturnActionDto> Handle(AddMerchandiseReturnActionCommand request,
        CancellationToken cancellationToken)
    {
        var action = request.Model.ToEntity();
        await _merchandiseReturnService.InsertMerchandiseReturnAction(action);
        return action.ToModel();
    }
}
