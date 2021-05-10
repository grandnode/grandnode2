using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class CancelOrderItemCommand : IRequest<(bool error, string message)>
    {
        public Order Order { get; set; }
        public OrderItem OrderItem { get; set; }
    }
}
