using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;

namespace Grand.Web.Commands.Handler.Orders;

public class WithdrawOrderApprovalCommandHandler : IRequestHandler<WithdrawOrderApprovalCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderService _orderService;

    public WithdrawOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService,
        IOrderService orderService, IMediator mediator)
    {
        _orderApprovalService = orderApprovalService;
        _orderService = orderService;
        _mediator = mediator;
    }

    public async Task<bool> Handle(WithdrawOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        var approval = await _orderApprovalService.GetById(request.ApprovalId,
            OrderApprovalRules.CompanyId(request.Customer));

        // a request can be withdrawn only by its own buyer, while it is still open
        if (approval == null || approval.CustomerId != request.Customer.Id || approval.StoreId != request.Store.Id ||
            approval.Status != OrderApprovalStatus.Pending)
            return false;

        var order = await _orderService.GetOrderById(approval.OrderId);
        if (order == null || order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return false;

        approval.Status = OrderApprovalStatus.Withdrawn;
        await _orderApprovalService.Update(approval);

        // the buyer withdrew it themselves, so no cancellation e-mail
        await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = false }, cancellationToken);
        return true;
    }
}
