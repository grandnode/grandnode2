using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class UpdateMerchandiseReturnReasonCommandHandler
    : IRequestHandler<UpdateMerchandiseReturnReasonCommand, MerchandiseReturnReasonDto>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public UpdateMerchandiseReturnReasonCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<MerchandiseReturnReasonDto> Handle(UpdateMerchandiseReturnReasonCommand request,
        CancellationToken cancellationToken)
    {
        var reason = await _merchandiseReturnService.GetMerchandiseReturnReasonById(request.Model.Id);
        reason = request.Model.ToEntity(reason);
        await _merchandiseReturnService.UpdateMerchandiseReturnReason(reason);
        return reason.ToModel();
    }
}
