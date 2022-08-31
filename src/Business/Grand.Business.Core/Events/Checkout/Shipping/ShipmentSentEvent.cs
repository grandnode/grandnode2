using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Shipping
{
    /// <summary>
    /// Shipment sent event
    /// </summary>
    public class ShipmentSentEvent : INotification
    {
        public ShipmentSentEvent(Shipment shipment)
        {
            Shipment = shipment;
        }

        /// <summary>
        /// Shipment
        /// </summary>
        public Shipment Shipment { get; private set; }
    }
}
