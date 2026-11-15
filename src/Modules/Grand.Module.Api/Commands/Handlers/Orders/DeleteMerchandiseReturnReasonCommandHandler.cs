using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class DeleteMerchandiseReturnReasonCommandHandler
    : IRequestHandler<DeleteMerchandiseReturnReasonCommand, bool>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public DeleteMerchandiseReturnReasonCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<bool> Handle(DeleteMerchandiseReturnReasonCommand request, CancellationToken cancellationToken)
    {
        var reason = await _merchandiseReturnService.GetMerchandiseReturnReasonById(request.Model.Id);
        if (reason != null)
            await _merchandiseReturnService.DeleteMerchandiseReturnReason(reason);

        return true;
    }
}
