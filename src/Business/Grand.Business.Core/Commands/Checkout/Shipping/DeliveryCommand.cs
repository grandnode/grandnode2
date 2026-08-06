using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Shipping;

public class DeliveryCommand : IRequest<bool>
{
    public Shipment Shipment { get; set; }
    public bool NotifyCustomer { get; set; }
}