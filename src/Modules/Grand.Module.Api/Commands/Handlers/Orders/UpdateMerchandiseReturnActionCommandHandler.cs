using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class UpdateMerchandiseReturnActionCommandHandler
    : IRequestHandler<UpdateMerchandiseReturnActionCommand, MerchandiseReturnActionDto>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public UpdateMerchandiseReturnActionCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<MerchandiseReturnActionDto> Handle(UpdateMerchandiseReturnActionCommand request,
        CancellationToken cancellationToken)
    {
        var action = await _merchandiseReturnService.GetMerchandiseReturnActionById(request.Model.Id);
        action = request.Model.ToEntity(action);
        await _merchandiseReturnService.UpdateMerchandiseReturnAction(action);
        return action.ToModel();
    }
}
