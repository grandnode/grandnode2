using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class CanPartiallyRefundOfflineQuery : IRequest<bool>
{
    public PaymentTransaction PaymentTransaction { get; set; }
    public double AmountToRefund { get; set; }
}