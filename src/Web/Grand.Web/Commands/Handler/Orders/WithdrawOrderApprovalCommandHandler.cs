using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Commands.Handler.Orders;

public class WithdrawOrderApprovalCommandHandler : IRequestHandler<WithdrawOrderApprovalCommand, bool>
{
    private readonly ILogger<WithdrawOrderApprovalCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderService _orderService;

    public WithdrawOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService,
        IOrderService orderService, IMediator mediator, ILogger<WithdrawOrderApprovalCommandHandler> logger)
    {
        _orderApprovalService = orderApprovalService;
        _orderService = orderService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(WithdrawOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        var approval = string.IsNullOrEmpty(request.OrderId)
            ? await _orderApprovalService.GetById(request.ApprovalId, OrderApprovalRules.CompanyId(request.Customer))
            : await GetStoreDecidedRequest(request.OrderId);

        // a request can be withdrawn only by its own buyer, while it is still open
        if (approval == null || approval.CustomerId != request.Customer.Id || approval.StoreId != request.Store.Id ||
            approval.Status != OrderApprovalStatus.Pending)
            return false;

        var order = await _orderService.GetOrderById(approval.OrderId);
        if (order == null || order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return false;

        // the request closes first: a decision taken at the same moment wins or loses as a whole
        if (!await _orderApprovalService.Close(approval, OrderApprovalStatus.Withdrawn))
            return false;

        // the buyer withdrew it themselves, so no cancellation e-mail
        try
        {
            await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = false }, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Order approval {Id}: the order could not be cancelled, the request goes back to pending",
                approval.Id);
            await _orderApprovalService.Reopen(approval);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     A company's request is withdrawn through the company's approvals screen, which also checks the company
    /// </summary>
    private async Task<OrderApproval> GetStoreDecidedRequest(string orderId)
    {
        var approval = await _orderApprovalService.GetByOrderId(orderId);
        return approval is { Decider: OrderApprovalDecider.Store } ? approval : null;
    }
}
