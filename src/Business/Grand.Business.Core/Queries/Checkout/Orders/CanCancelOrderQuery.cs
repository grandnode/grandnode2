using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class CanCancelOrderQuery : IRequest<bool>
{
    public required Order Order { get; set; }
}