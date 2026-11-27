using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class CanPartiallyRefundQuery : IRequest<bool>
{
    public double AmountToRefund { get; set; }
    public required PaymentTransaction PaymentTransaction { get; set; }
}