using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class DeleteMerchandiseReturnCommandHandler : IRequestHandler<DeleteMerchandiseReturnCommand, string?>
{
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;

    public DeleteMerchandiseReturnCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
    }

    public Task<string?> Handle(DeleteMerchandiseReturnCommand request, CancellationToken cancellationToken)
    {
        return _merchandiseReturnWorkflowService.Delete(request.Id);
    }
}
