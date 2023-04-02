using Grand.Domain.Payments;

namespace Payments.PayPalStandard
{
    /// <summary>
    /// Represents paypal helper
    /// </summary>
    public static class PaypalHelper
    {
        public static string OrderTotalSentToPayPal => "OrderTotalSentToPayPal";

        public static string PayPalUrlSandbox => "https://www.sandbox.paypal.com/us/cgi-bin/webscr";
        public static string PayPalUrl => "https://www.paypal.com/us/cgi-bin/webscr";

        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="paymentStatus">PayPal payment status</param>
        /// <param name="pendingReason">PayPal pending reason</param>
        /// <returns>Payment status</returns>
        public static PaymentStatus GetPaymentStatus(string paymentStatus, string pendingReason)
        {
            var result = PaymentStatus.Pending;

            paymentStatus ??= string.Empty;

            pendingReason ??= string.Empty;

            switch (paymentStatus.ToLowerInvariant())
            {
                case "pending":
                    result = pendingReason.ToLowerInvariant() switch {
                        "authorization" => PaymentStatus.Authorized,
                        _ => PaymentStatus.Pending
                    };
                    break;
                case "processed":
                case "completed":
                case "canceled_reversal":
                    result = PaymentStatus.Paid;
                    break;
                case "denied":
                case "expired":
                case "failed":
                case "voided":
                    result = PaymentStatus.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = PaymentStatus.Refunded;
                    break;
            }
            return result;
        }
    }
}

