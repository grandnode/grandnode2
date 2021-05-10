using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Events.Orders
{
    /// <summary>
    /// Order refunded event
    /// </summary>
    public class PaymentTransactionRefundedEvent : INotification
    {
        public PaymentTransactionRefundedEvent(PaymentTransaction paymentTransaction, decimal amount)
        {
            PaymentTransaction = PaymentTransaction;
            Amount = amount;
        }

        /// <summary>
        /// Order
        /// </summary>
        public PaymentTransaction PaymentTransaction { get; private set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; private set; }
    }
}
