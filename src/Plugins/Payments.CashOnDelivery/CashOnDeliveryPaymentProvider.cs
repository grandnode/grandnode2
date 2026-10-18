using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;

namespace Payments.CashOnDelivery;

public class CashOnDeliveryPaymentProvider : IPaymentProvider
{
    private readonly CashOnDeliveryPaymentSettings _cashOnDeliveryPaymentSettings;
    private readonly IContextAccessor _contextAccessor;
    private readonly ICurrencyService _currencyService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly IRequestTranslationService _translationService;

    public CashOnDeliveryPaymentProvider(
        IRequestTranslationService translationService,
        IShoppingCartSubTotalService shoppingCartSubTotalService,
        ICurrencyService currencyService,
        IContextAccessor contextAccessor,
        IPaymentTransactionService paymentTransactionService,
        CashOnDeliveryPaymentSettings cashOnDeliveryPaymentSettings)
    {
        _translationService = translationService;
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _currencyService = currencyService;
        _contextAccessor = contextAccessor;
        _paymentTransactionService = paymentTransactionService;
        _cashOnDeliveryPaymentSettings = cashOnDeliveryPaymentSettings;
    }

    public string ConfigurationUrl => CashOnDeliveryPaymentDefaults.ConfigurationUrl;

    public string SystemName => CashOnDeliveryPaymentDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(CashOnDeliveryPaymentDefaults.FriendlyName);

    public string GetFriendlyName(string languageId) =>
        _translationService.GetResource(CashOnDeliveryPaymentDefaults.FriendlyName, languageId, returnEmptyIfNotFound: true);

    public int Priority => _cashOnDeliveryPaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    /// <summary>
    ///     Init a process a payment transaction
    /// </summary>
    /// <returns>Payment transaction</returns>
    public async Task<PaymentTransaction> InitPaymentTransaction()
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    public async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        var result = new ProcessPaymentResult {
            NewPaymentTransactionStatus = TransactionStatus.Pending
        };
        return await Task.FromResult(result);
    }

    public Task PostProcessPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Post redirect payment (used by payment gateways that redirecting to a another URL)
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    public Task<string> PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.FromResult(string.Empty);
    }

    public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
    {
        if (_cashOnDeliveryPaymentSettings.ShippableProductRequired && !cart.RequiresShipping())
            return true;

        return await Task.FromResult(false);
    }

    public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
    {
        if (_cashOnDeliveryPaymentSettings.AdditionalFee <= 0)
            return _cashOnDeliveryPaymentSettings.AdditionalFee;

        if (_cashOnDeliveryPaymentSettings.AdditionalFeePercentage)
        {
            //percentage of the subtotal, which is already in the working currency
            var subtotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart,
                new CalculationContext(_contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore,
                _contextAccessor.WorkContext.WorkingCurrency, _contextAccessor.WorkContext.TaxDisplayType), true);
            return (float)subtotal.subTotalWithDiscount * (float)_cashOnDeliveryPaymentSettings.AdditionalFee / 100f;
        }

        //fixed value, kept in the primary store currency
        return await _currencyService.ConvertFromPrimaryStoreCurrency(_cashOnDeliveryPaymentSettings.AdditionalFee,
            _contextAccessor.WorkContext.WorkingCurrency);
    }

    public async Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model)
    {
        var warnings = new List<string>();
        return await Task.FromResult(warnings);
    }

    public async Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model)
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    public Task<string> GetControllerRouteName()
    {
        return Task.FromResult("Plugin.PaymentCashOnDelivery");
    }

    public async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
    {
        var result = new CapturePaymentResult();
        result.AddError("Capture method not supported");
        return await Task.FromResult(result);
    }

    public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        //it's not a redirection payment method. So we always return false
        return await Task.FromResult(false);
    }

    public async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();
        result.AddError("Refund method not supported");
        return await Task.FromResult(result);
    }

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
    public async Task CancelPayment(PaymentTransaction paymentTransaction)
    {
        paymentTransaction.TransactionStatus = TransactionStatus.Canceled;
        await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
    }

    public async Task<bool> SupportCapture()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportPartiallyRefund()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportRefund()
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SupportVoid()
    {
        return await Task.FromResult(false);
    }

    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    public async Task<bool> SkipPaymentInfo()
    {
        return await Task.FromResult(_cashOnDeliveryPaymentSettings.SkipPaymentInfo);
    }

    public async Task<string> Description()
    {
        return await Task.FromResult(
            _translationService.GetResource("Plugins.Payment.CashOnDelivery.PaymentMethodDescription"));
    }

    public string LogoURL => "/Plugins/Payments.CashOnDelivery/logo.jpg";
}