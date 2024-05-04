using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Shipping;

public class ShipCommandHandler : IRequestHandler<ShipCommand, bool>
{
    private readonly IMediator _mediator;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IOrderService _orderService;
    private readonly IShipmentService _shipmentService;

    public ShipCommandHandler(
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

    public async Task<bool> Handle(ShipCommand request, CancellationToken cancellationToken)
    {
        if (request.Shipment == null)
            throw new ArgumentNullException(nameof(request.Shipment));

        var order = await _orderService.GetOrderById(request.Shipment.OrderId);
        if (order == null)
            throw new Exception("Order cannot be loaded");

        if (request.Shipment.ShippedDateUtc.HasValue)
            throw new Exception("This shipment is already shipped");

        request.Shipment.ShippedDateUtc = DateTime.UtcNow;
        await _shipmentService.UpdateShipment(request.Shipment);

        //check whether we have more items to ship
        if (order.HasItemsToAddToShipment())
        {
            order.ShippingStatusId = ShippingStatus.PartiallyShipped;
        }
        else
        {
            var shipments = await _shipmentService.GetShipmentsByOrder(request.Shipment.OrderId);
            order.ShippingStatusId = shipments.All(x => x.ShippedDateUtc != null)
                ? ShippingStatus.Shipped
                : ShippingStatus.PartiallyShipped;
        }

        await _orderService.UpdateOrder(order);

        //add a note
        await _orderService.InsertOrderNote(new OrderNote {
            Note = $"Shipment #{request.Shipment.ShipmentNumber} has been sent",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        if (request.NotifyCustomer)
            //notify customer
            await _messageProviderService.SendShipmentSentCustomerMessage(request.Shipment, order);
        //check order status
        await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

        //event
        await _mediator.PublishShipmentSent(request.Shipment);

        return true;
    }
}