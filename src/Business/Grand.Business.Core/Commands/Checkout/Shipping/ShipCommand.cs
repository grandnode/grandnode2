using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Shipping;

public class ShipCommand : IRequest<bool>
{
    public Shipment Shipment { get; set; }
    public bool NotifyCustomer { get; set; }

    /// <summary>When a shipment is marked as shipped with a date of its own; null stamps now.</summary>
    public DateTime? ShippedDateUtc { get; set; }
}