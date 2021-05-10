using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class CanMarkPaymentTransactionAsAuthorizedQuery : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
