using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetPickupPointById : IRequest<PickupPoint?>
{
    public required string Id { get; set; }
}