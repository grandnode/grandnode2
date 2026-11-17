using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class ChangeMerchandiseReturnStatusCommandHandler : IRequestHandler<ChangeMerchandiseReturnStatusCommand, string>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;

    public ChangeMerchandiseReturnStatusCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        IMerchandiseReturnService merchandiseReturnService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
        _merchandiseReturnService = merchandiseReturnService;
    }

    public async Task<string> Handle(ChangeMerchandiseReturnStatusCommand request, CancellationToken cancellationToken)
    {
        var status = (MerchandiseReturnStatus)request.Status;
        //received by hand only where no return receipt takes the goods in, or one did already, as in the panel: the
        //goods of a product tracking stock reach the stock and their inspection through a receipt
        if (status == MerchandiseReturnStatus.Received)
        {
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(request.Id);
            if (merchandiseReturn == null)
                return MerchandiseReturnErrors.NotFound;
            if (!await _merchandiseReturnWorkflowService.CanMarkReceived(merchandiseReturn))
                return MerchandiseReturnErrors.StatusChangeNotAllowed;
        }

        return await _merchandiseReturnWorkflowService.ChangeStatus(request.Id, status, request.CustomerNote,
            request.ChangedBy);
    }
}
