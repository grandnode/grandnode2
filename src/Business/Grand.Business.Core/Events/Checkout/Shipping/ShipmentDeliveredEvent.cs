using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.Shipping;

/// <summary>
///     Shipment delivered event
/// </summary>
public class ShipmentDeliveredEvent : INotification
{
    public ShipmentDeliveredEvent(Shipment shipment)
    {
        Shipment = shipment;
    }

    /// <summary>
    ///     Shipment
    /// </summary>
    public Shipment Shipment { get; private set; }
}