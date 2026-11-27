using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class ChangeMerchandiseReturnStatusCommandHandler : IRequestHandler<ChangeMerchandiseReturnStatusCommand, string?>
{
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;

    public ChangeMerchandiseReturnStatusCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
    }

    public Task<string?> Handle(ChangeMerchandiseReturnStatusCommand request, CancellationToken cancellationToken)
    {
        return _merchandiseReturnWorkflowService.ChangeStatus(request.Id, (MerchandiseReturnStatus)request.Status,
            request.CustomerNote, request.ChangedBy);
    }
}
