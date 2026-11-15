using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class DeleteMerchandiseReturnActionCommandHandler
    : IRequestHandler<DeleteMerchandiseReturnActionCommand, bool>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;

    public DeleteMerchandiseReturnActionCommandHandler(IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<bool> Handle(DeleteMerchandiseReturnActionCommand request, CancellationToken cancellationToken)
    {
        var action = await _merchandiseReturnService.GetMerchandiseReturnActionById(request.Model.Id);
        if (action != null)
            await _merchandiseReturnService.DeleteMerchandiseReturnAction(action);

        return true;
    }
}
