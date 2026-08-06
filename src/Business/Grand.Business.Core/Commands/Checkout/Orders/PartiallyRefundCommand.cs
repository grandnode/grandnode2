using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PartiallyRefundCommand : IRequest<IList<string>>
{
    public PaymentTransaction PaymentTransaction { get; set; }
    public double AmountToRefund { get; set; }
}