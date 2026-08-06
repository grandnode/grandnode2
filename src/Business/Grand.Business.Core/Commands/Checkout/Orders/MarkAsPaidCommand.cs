using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class MarkAsPaidCommand : IRequest<bool>
{
    public PaymentTransaction PaymentTransaction { get; set; }
}