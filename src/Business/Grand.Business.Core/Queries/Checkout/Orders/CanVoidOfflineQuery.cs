using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class CanVoidOfflineQuery : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}

