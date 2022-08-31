using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class CanPartiallyPaidOfflineQuery : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
        public double AmountToPaid { get; set; }
    }
}
