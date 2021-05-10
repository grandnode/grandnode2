using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class PartiallyRefundOfflineCommand : IRequest<bool>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
        public decimal AmountToRefund { get; set; }
    }
}
