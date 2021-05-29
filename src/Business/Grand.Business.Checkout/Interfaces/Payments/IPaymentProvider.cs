using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Utilities;
using Grand.Infrastructure.Plugins;
using Grand.Domain.Orders;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grand.Domain.Payments;

namespace Grand.Business.Checkout.Interfaces.Payments
{
    /// <summary>
    /// Provides an interface for creating payment gateways & methods
    /// </summary>
    public partial interface IPaymentProvider : IProvider
    {
        #region Methods

        /// <summary>
        /// Init a process a payment transaction
        /// </summary>
        /// <returns>Payment transaction</returns>
        Task<PaymentTransaction> InitPaymentTransaction();

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Process payment result</returns>
        Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Post process payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        Task PostProcessPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Post redirect payment (used by payment gateways that redirecting to a another URL)
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        Task PostRedirectPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Capture payment result</returns>
        Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest);

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Result</returns>
        Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Cancel payment transaction
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        Task CancelPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="PaymentTransaction">PaymentTransaction</param>
        /// <returns>Result</returns>
        Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        Task<IList<string>> ValidatePaymentForm(IFormCollection form);

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        Task<PaymentTransaction> SavePaymentInfo(IFormCollection form);

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        void GetPublicViewComponent(out string viewComponentName);

        #endregion

        #region Properties

      
        /// <summary>
        /// Gets a payment method type
        /// </summary>
        PaymentMethodType PaymentMethodType { get; }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        Task<bool> SkipPaymentInfo();

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        Task<string> Description();

        /// <summary>
        /// Gets a logo URL
        /// </summary>
        string LogoURL { get; }

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        Task<bool> SupportCapture();

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        Task<bool> SupportPartiallyRefund();

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        Task<bool> SupportRefund();

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        Task<bool> SupportVoid();

        #endregion
    }
}
