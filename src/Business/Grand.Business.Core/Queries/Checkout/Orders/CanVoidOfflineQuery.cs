using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class CanVoidOfflineQuery : IRequest<bool>
{
    public required PaymentTransaction PaymentTransaction { get; set; }
}