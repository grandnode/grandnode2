using Grand.Domain.Payments;

namespace Grand.Business.Checkout.Utilities
{
    /// <summary>
    /// Represents a RefundPaymentResult
    /// </summary>
    public partial class RefundPaymentRequest
    {
        /// <summary>
        /// Gets or sets an payment transaction
        /// </summary>
        public PaymentTransaction PaymentTransaction { get; set; }

        /// <summary>
        /// Gets or sets an amount
        /// </summary>
        public double AmountToRefund { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it's a partial refund; otherwize, full refund
        /// </summary>
        public bool IsPartialRefund { get; set; }
    }
}
