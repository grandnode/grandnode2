using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class CheckOrderStatusCommandHandler : IRequestHandler<CheckOrderStatusCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly OrderSettings _orderSettings;

    public CheckOrderStatusCommandHandler(
        IMediator mediator,
        IOrderService orderService,
        OrderSettings orderSettings)
    {
        _mediator = mediator;
        _orderService = orderService;
        _orderSettings = orderSettings;
    }

    public async Task<bool> Handle(CheckOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.Order == null)
            throw new ArgumentNullException(nameof(request.Order));

        if (request.Order.PaymentStatusId == PaymentStatus.Paid && !request.Order.PaidDateUtc.HasValue)
        {
            //ensure that paid date is set
            request.Order.PaidDateUtc = DateTime.UtcNow;
            await _orderService.UpdateOrder(request.Order);
        }

        switch (request.Order.OrderStatusId)
        {
            case (int)OrderStatusSystem.Pending:
            {
                if (request.Order.PaymentStatusId is PaymentStatus.Authorized or PaymentStatus.Paid
                    or PaymentStatus.PartiallyPaid)
                    await _mediator.Send(new SetOrderStatusCommand {
                        Order = request.Order,
                        Os = OrderStatusSystem.Processing,
                        NotifyCustomer = false,
                        NotifyStoreOwner = false
                    }, cancellationToken);

                if (request.Order.ShippingStatusId is ShippingStatus.PartiallyShipped or ShippingStatus.Shipped
                    or ShippingStatus.Delivered)
                    await _mediator.Send(new SetOrderStatusCommand {
                        Order = request.Order,
                        Os = OrderStatusSystem.Processing,
                        NotifyCustomer = false,
                        NotifyStoreOwner = false
                    }, cancellationToken);

                break;
            }
            case (int)OrderStatusSystem.Cancelled or (int)OrderStatusSystem.Complete:
                return true;
        }

        if (request.Order.PaymentStatusId != PaymentStatus.Paid) return true;
        bool completed;
        if (request.Order.ShippingStatusId == ShippingStatus.ShippingNotRequired)
        {
            completed = true;
        }
        else
        {
            if (_orderSettings.CompleteOrderWhenDelivered)
                completed = request.Order.ShippingStatusId == ShippingStatus.Delivered;
            else
                completed = request.Order.ShippingStatusId is ShippingStatus.Shipped or ShippingStatus.Delivered;
        }

        if (completed)
            await _mediator.Send(new SetOrderStatusCommand {
                Order = request.Order,
                Os = OrderStatusSystem.Complete,
                NotifyCustomer = true,
                NotifyStoreOwner = false
            }, cancellationToken);
        return true;
    }
}