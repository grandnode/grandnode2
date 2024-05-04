using Grand.Business.Core.Events.Checkout.Shipping;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Extensions;

public static class ShippingEventPublisherExtensions
{
    /// <summary>
    ///     Publishes the shipment sent event.
    /// </summary>
    /// <param name="mediator">The event publisher.</param>
    /// <param name="shipment">The shipment.</param>
    public static async Task PublishShipmentSent(this IMediator mediator, Shipment shipment)
    {
        await mediator.Publish(new ShipmentSentEvent(shipment));
    }

    /// <summary>
    ///     Publishes the shipment delivered event.
    /// </summary>
    /// <param name="mediator">The event publisher.</param>
    /// <param name="shipment">The shipment.</param>
    public static async Task PublishShipmentDelivered(this IMediator mediator, Shipment shipment)
    {
        await mediator.Publish(new ShipmentDeliveredEvent(shipment));
    }
}