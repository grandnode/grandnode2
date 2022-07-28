using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Grand.Business.Core.Extensions;

namespace Grand.Business.Checkout.Commands.Handlers.Shipping
{
    public class DeliveryCommandHandler : IRequestHandler<DeliveryCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IShipmentService _shipmentService;
        private readonly IMessageProviderService _messageProviderService;

        public DeliveryCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IShipmentService shipmentService,
            IMessageProviderService messageProviderService)
        {
            _mediator = mediator;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(DeliveryCommand request, CancellationToken cancellationToken)
        {
            if (request.Shipment == null)
                throw new ArgumentNullException(nameof(request.Shipment));

            var order = await _orderService.GetOrderById(request.Shipment.OrderId);
            if (order == null)
                throw new Exception("Order cannot be loaded");

            if (!request.Shipment.ShippedDateUtc.HasValue)
                throw new Exception("This shipment is not shipped yet");

            if (request.Shipment.DeliveryDateUtc.HasValue)
                throw new Exception("This shipment is already delivered");

            request.Shipment.DeliveryDateUtc = DateTime.UtcNow;
            await _shipmentService.UpdateShipment(request.Shipment);

            var shipments = await _shipmentService.GetShipmentsByOrder(request.Shipment.OrderId);
            if (!order.HasItemsToAddToShipment() && !shipments.Where(x => x.DeliveryDateUtc == null).Any())
            {
                order.ShippingStatusId = ShippingStatus.Delivered;
                await _orderService.UpdateOrder(order);
            }

            //add a note
            await _orderService.InsertOrderNote(new OrderNote {
                Note = $"Shipment #{request.Shipment.ShipmentNumber} has been delivered",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });
            if (request.NotifyCustomer)
            {
                //send email notification
                await _messageProviderService.SendShipmentDeliveredCustomerMessage(request.Shipment, order);
            }
            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = order });

            //event
            await _mediator.PublishShipmentDelivered(request.Shipment);

            return true;
        }
    }
}
