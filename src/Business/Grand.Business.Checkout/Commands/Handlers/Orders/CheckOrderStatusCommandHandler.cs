using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
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

            if (request.Order.OrderStatusId == (int)OrderStatusSystem.Pending)
            {
                if (request.Order.PaymentStatusId == PaymentStatus.Authorized ||
                    request.Order.PaymentStatusId == PaymentStatus.Paid ||
                    request.Order.PaymentStatusId == PaymentStatus.PartiallyPaid
                    )
                {
                    await _mediator.Send(new SetOrderStatusCommand
                    {
                        Order = request.Order,
                        Os = OrderStatusSystem.Processing,
                        NotifyCustomer = false,
                        NotifyStoreOwner = false
                    });
                }

                if (request.Order.ShippingStatusId == ShippingStatus.PartiallyShipped ||
                    request.Order.ShippingStatusId == ShippingStatus.Shipped ||
                    request.Order.ShippingStatusId == ShippingStatus.Delivered)
                {
                    await _mediator.Send(new SetOrderStatusCommand
                    {
                        Order = request.Order,
                        Os = OrderStatusSystem.Processing,
                        NotifyCustomer = false,
                        NotifyStoreOwner = false
                    });
                }
            }

            if (request.Order.OrderStatusId != (int)OrderStatusSystem.Cancelled &&
                request.Order.OrderStatusId != (int)OrderStatusSystem.Complete)
            {
                if (request.Order.PaymentStatusId == PaymentStatus.Paid)
                {
                    bool completed;
                    if (request.Order.ShippingStatusId == ShippingStatus.ShippingNotRequired)
                    {
                        completed = true;
                    }
                    else
                    {
                        if (_orderSettings.CompleteOrderWhenDelivered)
                        {
                            completed = request.Order.ShippingStatusId == ShippingStatus.Delivered;
                        }
                        else
                        {
                            completed = request.Order.ShippingStatusId == ShippingStatus.Shipped ||
                                request.Order.ShippingStatusId == ShippingStatus.Delivered;
                        }
                    }
                    if (completed)
                    {
                        await _mediator.Send(new SetOrderStatusCommand
                        {
                            Order = request.Order,
                            Os = OrderStatusSystem.Complete,
                            NotifyCustomer = true,
                            NotifyStoreOwner = false
                        });
                    }
                }
            }
            return true;
        }
    }
}
