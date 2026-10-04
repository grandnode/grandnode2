using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class CheckOrderStatusCommand : IRequest<bool>
{
    public Order Order { get; set; }
}