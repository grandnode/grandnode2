using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;

namespace Grand.Web.Commands.Handler.Orders;

public class WithdrawOrderApprovalCommandHandler : IRequestHandler<WithdrawOrderApprovalCommand, bool>
{
    private readonly IOrderApprovalService _orderApprovalService;

    public WithdrawOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService)
    {
        _orderApprovalService = orderApprovalService;
    }

    public async Task<bool> Handle(WithdrawOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        var approval = await _orderApprovalService.GetById(request.ApprovalId,
            OrderApprovalRules.CompanyId(request.Customer));

        // a request can be withdrawn only by its own buyer, while it is still open or waiting to be used
        if (approval == null || approval.CustomerId != request.Customer.Id || approval.StoreId != request.Store.Id ||
            approval.Status is not (OrderApprovalStatus.Pending or OrderApprovalStatus.Approved))
            return false;

        approval.Status = OrderApprovalStatus.Withdrawn;
        await _orderApprovalService.Update(approval);
        return true;
    }
}
