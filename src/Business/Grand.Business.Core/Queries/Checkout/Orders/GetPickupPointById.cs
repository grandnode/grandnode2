using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class GetPickupPointById : IRequest<PickupPoint>
    {
        public string Id { get; set; }
    }
}
