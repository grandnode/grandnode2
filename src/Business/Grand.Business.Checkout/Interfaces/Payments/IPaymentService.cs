using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Utilities;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Payments
{
    /// <summary>
    /// Payment service interface
    /// </summary>
    public partial interface IPaymentService
    {
        /// <summary>
        /// Load active payment methods
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="filterByCountryId">Specified country</param>
        /// <returns>Payment methods</returns>
        Task<IList<IPaymentProvider>> LoadActivePaymentMethods(Customer customer = null, string storeId = "", string filterByCountryId = "");

        /// <summary>
        /// Load payment provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found payment provider</returns>
        IPaymentProvider LoadPaymentMethodBySystemName(string systemName);

        /// <summary>
        /// Load all payment providers
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <param name="filterByCountryId">Specified country</param>
        /// <returns>Payment providers</returns>
        IList<IPaymentProvider> LoadAllPaymentMethods(Customer customer = null, string storeId = "", string filterByCountryId = "");

        /// <summary>
        /// Gets a list of coutnry identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>A list of country identifiers</returns>
        IList<string> GetRestrictedCountryIds(IPaymentProvider paymentMethod);

        /// <summary>
        /// Gets a list of shipping identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>A list of role identifiers</returns>
        IList<string> GetRestrictedShippingIds(IPaymentProvider paymentMethod);

        /// <summary>
        /// Saves a list of country identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="countryIds">A list of country identifiers</param>
        Task SaveRestictedCountryIds(IPaymentProvider paymentMethod, List<string> countryIds);

        /// <summary>
        /// Saves a list of shipping identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="shippingIds">A list of shipping identifiers</param>
        Task SaveRestictedShippingIds(IPaymentProvider paymentMethod, List<string> shippingIds);

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
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Result</returns>
        Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Gets an additional handling fee of a payment method
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>Additional handling fee</returns>
        Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, string paymentMethodSystemName);

        /// <summary>
        /// Gets a value indicating whether capture is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether capture is supported</returns>
        Task<bool> SupportCapture(string paymentMethodSystemName);

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Capture payment result</returns>
        Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Gets a value indicating whether partial refund is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether partial refund is supported</returns>
        Task<bool> SupportPartiallyRefund(string paymentMethodSystemName);

        /// <summary>
        /// Gets a value indicating whether refund is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether refund is supported</returns>
        Task<bool> SupportRefund(string paymentMethodSystemName);

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest);

        /// <summary>
        /// Gets a value indicating whether void is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether void is supported</returns>
        Task<bool> SupportVoid(string paymentMethodSystemName);

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Result</returns>
        Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Cancel payment
        /// </summary>
        /// <param name="paymentTransaction"></param>
        /// <returns></returns>
        Task CancelPayment(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A payment method type</returns>
        PaymentMethodType GetPaymentMethodType(string paymentMethodSystemName);

    }
}
