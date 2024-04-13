using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Payments.CashOnDelivery;

public class CashOnDeliveryPaymentProvider : IPaymentProvider
{
    private readonly CashOnDeliveryPaymentSettings _cashOnDeliveryPaymentSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITranslationService _translationService;

    public CashOnDeliveryPaymentProvider(
        ITranslationService translationService,
        IHttpContextAccessor httpContextAccessor,
        CashOnDeliveryPaymentSettings cashOnDeliveryPaymentSettings)
    {
        _translationService = translationService;
        _httpContextAccessor = httpContextAccessor;
        _cashOnDeliveryPaymentSettings = cashOnDeliveryPaymentSettings;
    }

    public string ConfigurationUrl => CashOnDeliveryPaymentDefaults.ConfigurationUrl;

    public string SystemName => CashOnDeliveryPaymentDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(CashOnDeliveryPaymentDefaults.FriendlyName);

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
    public Task PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.CompletedTask;
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

        double result;
        if (_cashOnDeliveryPaymentSettings.AdditionalFeePercentage)
        {
            //percentage
            var orderTotalCalculationService = _httpContextAccessor.HttpContext!.RequestServices
                .GetRequiredService<IOrderCalculationService>();
            var subtotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
            result = (float)subtotal.subTotalWithDiscount * (float)_cashOnDeliveryPaymentSettings.AdditionalFee / 100f;
        }
        else
        {
            //fixed value
            result = _cashOnDeliveryPaymentSettings.AdditionalFee;
        }

        if (!(result > 0)) return result;
        var currencyService = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ICurrencyService>();
        var workContext = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IWorkContext>();
        result = await currencyService.ConvertFromPrimaryStoreCurrency(result, workContext.WorkingCurrency);

        //return result;
        return result;
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
        var paymentTransactionService = _httpContextAccessor.HttpContext!.RequestServices
            .GetRequiredService<IPaymentTransactionService>();
        paymentTransaction.TransactionStatus = TransactionStatus.Canceled;
        await paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
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