using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class CanCancelOrderQuery : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
