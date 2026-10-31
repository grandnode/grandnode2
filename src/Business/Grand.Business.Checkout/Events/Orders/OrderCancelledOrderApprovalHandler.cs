using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     Closes the approval request of an order cancelled before anyone decided it
/// </summary>
public class OrderCancelledOrderApprovalHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly ILogger<OrderCancelledOrderApprovalHandler> _logger;
    private readonly IOrderApprovalService _orderApprovalService;

    public OrderCancelledOrderApprovalHandler(IOrderApprovalService orderApprovalService,
        ILogger<OrderCancelledOrderApprovalHandler> logger)
    {
        _orderApprovalService = orderApprovalService;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        try
        {
            // a decision or a withdrawal closes the request before cancelling, so it is not pending here
            var approval = await _orderApprovalService.GetByOrderId(order.Id);
            if (approval is { Status: OrderApprovalStatus.Pending })
                await _orderApprovalService.Close(approval, OrderApprovalStatus.Cancelled);
        }
        catch (Exception ex)
        {
            // the order is already cancelled: publishing must complete whatever happens here
            _logger.LogError(ex,
                "Order {OrderNumber} ({OrderId}) was cancelled but its approval request could not be closed",
                order.OrderNumber, order.Id);
        }
    }
}
