using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    /// <summary>
    /// Order refunded event
    /// </summary>
    public class PaymentTransactionRefundedEvent : INotification
    {
        public PaymentTransactionRefundedEvent(PaymentTransaction paymentTransaction, double amount)
        {
            PaymentTransaction = paymentTransaction;
            Amount = amount;
        }

        /// <summary>
        /// Order
        /// </summary>
        public PaymentTransaction PaymentTransaction { get; private set; }

        /// <summary>
        /// Amount
        /// </summary>
        public double Amount { get; private set; }
    }
}
