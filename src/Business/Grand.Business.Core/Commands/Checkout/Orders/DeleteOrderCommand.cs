using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class DeleteOrderCommand : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
