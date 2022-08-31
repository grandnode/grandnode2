using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class CanPartiallyRefundQuery : IRequest<bool>
    {
        public double AmountToRefund { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
