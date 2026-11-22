using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Payments.StripeCheckout.Services;
using Grand.Web.Common.Localization;

namespace Payments.StripeCheckout;

public class StripeCheckoutPaymentProvider : IPaymentProvider
{
    private readonly IOrderService _orderService;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IStripeCheckoutService _stripeCheckoutService;
    private readonly IRequestTranslationService _translationService;

    #region Ctor

    public StripeCheckoutPaymentProvider(
        IRequestTranslationService translationService,
        IOrderService orderService,
        IRecurringPaymentService recurringPaymentService,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings, IStripeCheckoutService stripeCheckoutService)
    {
        _translationService = translationService;
        _orderService = orderService;
        _recurringPaymentService = recurringPaymentService;
        _stripeCheckoutPaymentSettings = stripeCheckoutPaymentSettings;
        _stripeCheckoutService = stripeCheckoutService;
    }

    #endregion

    public string ConfigurationUrl => StripeCheckoutDefaults.ConfigurationUrl;

    public string SystemName => StripeCheckoutDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(StripeCheckoutDefaults.FriendlyName);

    public string GetFriendlyName(string languageId) =>
        _translationService.GetResource(StripeCheckoutDefaults.FriendlyName, languageId, returnEmptyIfNotFound: true);

    public int Priority => _stripeCheckoutPaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    #region Properties

    /// <summary>
    ///     Init a process a payment transaction
    /// </summary>
    /// <returns>Payment transaction</returns>
    public async Task<PaymentTransaction> InitPaymentTransaction()
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    /// <summary>
    ///     Process a payment
    /// </summary>
    /// <param name="paymentTransaction"></param>
    /// <returns>Process payment result</returns>
    public async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        var result = new ProcessPaymentResult();
        return await Task.FromResult(result);
    }

    public Task PostProcessPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="paymentTransaction"></param>
    public async Task<string> PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
        return await _stripeCheckoutService.CreateRedirectUrl(order);
    }

    /// <summary>
    ///     Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this payment method if all products in the cart are downloadable
        //or hide this payment method if current customer is from certain country
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>Additional handling fee</returns>
    public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
    {
        return await Task.FromResult(0.0);
    }

    /// <summary>
    ///     Captures payment
    /// </summary>
    /// <returns>Capture payment result</returns>
    public async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
    {
        var result = new CapturePaymentResult();
        result.AddError("Capture method not supported");
        return await Task.FromResult(result);
    }

    /// <summary>
    ///     Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult {
            //a partial refund leaves the status to the handler, which compares the refunded amount with the total
            NewTransactionStatus = refundPaymentRequest.IsPartialRefund
                ? TransactionStatus.Pending
                : TransactionStatus.Refunded
        };
        //a full refund leaves the amount to Stripe: the paid amount recorded here is the order total, which
        //may round a cent away from what Stripe charged, and Stripe refuses a refund above the charge
        var errors = await _stripeCheckoutService.Refund(refundPaymentRequest.PaymentTransaction,
            refundPaymentRequest.IsPartialRefund ? refundPaymentRequest.AmountToRefund : null);
        foreach (var error in errors)
            result.AddError(error);

        return result;
    }

    /// <summary>
    ///     Voids a payment
    /// </summary>
    /// <returns>Result</returns>
    public async Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
    {
        var result = new VoidPaymentResult();
        result.AddError("Void method not supported");
        return await Task.FromResult(result);
    }

    /// <summary>
    ///     Cancel a payment
    /// </summary>
    /// <returns>Result</returns>
    public Task CancelPayment(PaymentTransaction paymentTransaction)
    {
        return Task.CompletedTask;
    }


    /// <summary>
    ///     Gets a value indicating whether customers can complete a payment after order is placed but not completed (for
    ///     redirection payment methods)
    /// </summary>
    /// <returns>Result</returns>
    public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);
        if ((DateTime.UtcNow - paymentTransaction.CreatedOnUtc).TotalMinutes < 1)
            return false;

        //the customer completed the checkout and Stripe runs the subscription, whose first invoice is still to be
        //reported: paying again would start a second subscription charging every cycle twice
        var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
        if (order != null &&
            await _recurringPaymentService.GetRecurringPaymentByInitialOrderId(order.Id) is { } recurringPayment &&
            !string.IsNullOrEmpty(recurringPayment.ProviderReference))
            return false;

        return true;
    }

    /// <summary>
    ///     Validate payment form
    /// </summary>
    /// <param name="model"></param>
    /// <returns>List of validating errors</returns>
    public async Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model)
    {
        return await Task.FromResult(new List<string>());
    }

    /// <summary>
    ///     Get payment information
    /// </summary>
    /// <param name="model"></param>
    /// <returns>Payment info holder</returns>
    public async Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model)
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }


    /// <summary>
    ///     Gets a value indicating whether capture is supported
    /// </summary>
    public async Task<bool> SupportCapture()
    {
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a value indicating whether partial refund is supported
    /// </summary>
    public async Task<bool> SupportPartiallyRefund()
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    ///     Gets a value indicating whether refund is supported
    /// </summary>
    public async Task<bool> SupportRefund()
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    ///     Gets a value indicating whether void is supported
    /// </summary>
    public async Task<bool> SupportVoid()
    {
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a value indicating whether recurring payments are supported: a recurring order is paid with a Stripe
    ///     subscription, whose invoices report every cycle
    /// </summary>
    public Task<bool> SupportRecurringPayment()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Cancels the Stripe subscription of a recurring payment
    /// </summary>
    public async Task<CancelRecurringPaymentResult> CancelRecurringPayment(RecurringPayment recurringPayment)
    {
        var result = new CancelRecurringPaymentResult();
        foreach (var error in await _stripeCheckoutService.CancelSubscription(recurringPayment))
            result.AddError(error);

        return result;
    }

    /// <summary>
    ///     Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

    /// <summary>
    ///     Gets a value indicating whether we should display a payment information page for this plugin
    /// </summary>
    public async Task<bool> SkipPaymentInfo()
    {
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    public async Task<string> Description()
    {
        return await Task.FromResult(_translationService.GetResource("Plugins.Payments.StripeCheckout.FriendlyName"));
    }

    public Task<string> GetControllerRouteName()
    {
        return Task.FromResult(StripeCheckoutDefaults.PaymentInfo);
    }

    public string LogoURL => "/Plugins/Payments.StripeCheckout/logo.jpg";

    #endregion
}