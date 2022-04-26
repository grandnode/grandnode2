using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class CanVoidQuery : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
