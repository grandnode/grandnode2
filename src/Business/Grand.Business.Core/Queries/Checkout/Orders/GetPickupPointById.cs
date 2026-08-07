using Grand.Domain.Shipping;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetPickupPointById : IRequest<PickupPoint>
{
    public string Id { get; set; }
}