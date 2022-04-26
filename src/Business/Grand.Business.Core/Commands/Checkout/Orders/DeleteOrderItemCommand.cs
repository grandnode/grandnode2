using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class DeleteOrderItemCommand : IRequest<(bool error, string message)>
    {
        public Order Order { get; set; }
        public OrderItem OrderItem { get; set; }
    }
}
