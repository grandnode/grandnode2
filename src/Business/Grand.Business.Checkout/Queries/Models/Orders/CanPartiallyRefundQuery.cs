using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class CanPartiallyRefundQuery : IRequest<bool>
    {
        public double AmountToRefund { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
