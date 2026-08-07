using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class CanRefundQuery : IRequest<bool>
{
    public PaymentTransaction PaymentTransaction { get; set; }
}