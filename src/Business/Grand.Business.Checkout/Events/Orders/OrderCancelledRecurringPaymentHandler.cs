using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     Cancels the recurring payment of an initial order that was cancelled or deleted
/// </summary>
public class OrderCancelledRecurringPaymentHandler : INotificationHandler<OrderCancelledEvent>,
    INotificationHandler<OrderDeletedEvent>
{
    private readonly ILogger<OrderCancelledRecurringPaymentHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IRecurringPaymentService _recurringPaymentService;

    public OrderCancelledRecurringPaymentHandler(IRecurringPaymentService recurringPaymentService,
        IOrderService orderService, IMediator mediator, ILogger<OrderCancelledRecurringPaymentHandler> logger)
    {
        _recurringPaymentService = recurringPaymentService;
        _orderService = orderService;
        _mediator = mediator;
        _logger = logger;
    }

    public Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        return CancelRecurringPayment(notification.Order, "cancelled");
    }

    public Task Handle(OrderDeletedEvent notification, CancellationToken cancellationToken)
    {
        return CancelRecurringPayment(notification.Order, "deleted");
    }

    private async Task CancelRecurringPayment(Order order, string what)
    {
        // a cycle order or a plain order never owns a recurring payment, and a plain order costs no query
        if (order.RecurringCycleNumber != 1) return;

        try
        {
            var recurringPayment = await _recurringPaymentService.GetRecurringPaymentByInitialOrderId(order.Id);
            if (recurringPayment == null) return;

            var result = await _mediator.Send(new CancelRecurringPaymentCommand {
                RecurringPayment = recurringPayment,
                CancelledBy = $"Order #{order.OrderNumber} {what}"
            });
            if (!result.Success)
                await AddNote(order, what, string.Join(", ", result.Errors));
        }
        catch (Exception ex)
        {
            // the order is already cancelled or deleted: publishing must complete whatever happens here
            _logger.LogError(ex,
                "Order {OrderNumber} ({OrderId}) was {What} but its recurring payment could not be cancelled",
                order.OrderNumber, order.Id, what);
            await AddNote(order, what, ex.Message);
        }
    }

    private async Task AddNote(Order order, string what, string reason)
    {
        try
        {
            await _orderService.InsertOrderNote(new OrderNote {
                OrderId = order.Id,
                Note =
                    $"The order was {what}, but its recurring payment could not be cancelled, so the payment gateway may still charge: {reason}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The note about the recurring payment of order {OrderId} could not be saved",
                order.Id);
        }
    }
}
