using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class DecideStoreOrderApprovalCommandHandler : IRequestHandler<DecideStoreOrderApprovalCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderApprovalService _orderApprovalService;

    public DecideStoreOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService, IMediator mediator)
    {
        _orderApprovalService = orderApprovalService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(DecideStoreOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Order);
        ArgumentNullException.ThrowIfNull(request.DecidedBy);

        // a company's request stays with the company: the store can only cancel the order
        var approval = await _orderApprovalService.GetByOrderId(request.Order.Id);
        if (approval is not { Decider: OrderApprovalDecider.Store })
            return false;

        return await _mediator.Send(new ApplyOrderApprovalDecisionCommand {
            Approval = approval,
            DecidedBy = request.DecidedBy,
            Approve = request.Approve,
            Comment = request.Comment,
            Language = request.Language
        }, cancellationToken);
    }
}
