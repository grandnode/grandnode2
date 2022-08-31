using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class PartiallyPaidOfflineCommand : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
        public double AmountToPaid { get; set; }
    }
}
