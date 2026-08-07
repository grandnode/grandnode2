using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class VoidCommand : IRequest<IList<string>>
{
    public PaymentTransaction PaymentTransaction { get; set; }
}