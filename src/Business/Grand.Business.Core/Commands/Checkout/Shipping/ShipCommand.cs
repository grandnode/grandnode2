using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Shipping
{
    public class ShipCommand : IRequest<bool>
    {
        public Shipment Shipment { get; set; }
        public bool NotifyCustomer { get; set; }
    }
}
