using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     Marks the approval that let the order through as used, so it cannot be spent on another order
/// </summary>
public class OrderPlacedOrderApprovalHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IOrderApprovalService _orderApprovalService;

    public OrderPlacedOrderApprovalHandler(IOrderApprovalService orderApprovalService)
    {
        _orderApprovalService = orderApprovalService;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        var approval = await _orderApprovalService.GetActive(order.CustomerId, order.StoreId);
        if (approval is not { Status: OrderApprovalStatus.Approved }) return;

        approval.Status = OrderApprovalStatus.Used;
        approval.OrderId = order.Id;
        await _orderApprovalService.Update(approval);
    }
}
