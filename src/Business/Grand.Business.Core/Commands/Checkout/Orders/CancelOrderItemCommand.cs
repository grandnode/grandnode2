using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class CancelOrderItemCommand : IRequest<(bool error, string message)>
{
    public Order Order { get; set; }
    public OrderItem OrderItem { get; set; }
}