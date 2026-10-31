using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;

namespace Grand.Web.Commands.Handler.Orders;

public class DecideOrderApprovalCommandHandler : IRequestHandler<DecideOrderApprovalCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderApprovalService _orderApprovalService;

    public DecideOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService, IMediator mediator)
    {
        _orderApprovalService = orderApprovalService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(DecideOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Approver);
        ArgumentNullException.ThrowIfNull(request.Store);

        // every refusal gives the same answer: a foreign, missing or finished request, an order that no longer
        // awaits the decision and a non-approver cannot be told apart
        if (!OrderApprovalRules.CanApprove(request.Approver))
            return false;

        var approval = await _orderApprovalService.GetById(request.ApprovalId,
            OrderApprovalRules.CompanyId(request.Approver));
        if (approval == null || approval.StoreId != request.Store.Id)
            return false;

        return await _mediator.Send(new ApplyOrderApprovalDecisionCommand {
            Approval = approval,
            DecidedBy = request.Approver,
            Approve = request.Approve,
            Comment = request.Comment,
            Language = request.Language,
            Host = request.Host
        }, cancellationToken);
    }
}
