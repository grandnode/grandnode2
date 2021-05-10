using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Events.Orders
{
    /// <summary>
    /// Order mark as authorized event
    /// </summary>
    public class PaymentTransactionMarkAsAuthorizedEvent : INotification
    {
        public PaymentTransactionMarkAsAuthorizedEvent(PaymentTransaction paymentTransaction)
        {
            PaymentTransaction = paymentTransaction;
        }

        /// <summary>
        /// Order
        /// </summary>
        public PaymentTransaction PaymentTransaction { get; private set; }
    }

}
