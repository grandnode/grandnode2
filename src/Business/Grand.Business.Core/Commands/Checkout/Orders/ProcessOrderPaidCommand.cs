using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ProcessOrderPaidCommand : IRequest<bool>
{
    public required Order Order { get; set; }
}