using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;

namespace Payments.OnAccount;

/// <summary>
///     Pay by invoice: the order is placed on the company's trade credit and paid later, within the payment term.
///     The method is offered only while the credit covers the cart, and the credit is checked again when the order
///     is placed, so a hidden method posted anyway is refused.
/// </summary>
public class OnAccountPaymentProvider : IPaymentProvider
{
    private readonly ICompanyCreditService _companyCreditService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly OnAccountPaymentSettings _onAccountPaymentSettings;
    private readonly IRequestTranslationService _translationService;

    public OnAccountPaymentProvider(
        IRequestTranslationService translationService,
        ICompanyCreditService companyCreditService,
        ICustomerService customerService,
        IShoppingCartSubTotalService shoppingCartSubTotalService,
        ICurrencyService currencyService,
        IContextAccessor contextAccessor,
        IPaymentTransactionService paymentTransactionService,
        OnAccountPaymentSettings onAccountPaymentSettings)
    {
        _translationService = translationService;
        _companyCreditService = companyCreditService;
        _customerService = customerService;
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _currencyService = currencyService;
        _contextAccessor = contextAccessor;
        _paymentTransactionService = paymentTransactionService;
        _onAccountPaymentSettings = onAccountPaymentSettings;
    }

    public string ConfigurationUrl => OnAccountPaymentDefaults.ConfigurationUrl;

    public string SystemName => OnAccountPaymentDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(OnAccountPaymentDefaults.FriendlyName);

    public string GetFriendlyName(string languageId) =>
        _translationService.GetResource(OnAccountPaymentDefaults.FriendlyName, languageId, returnEmptyIfNotFound: true);

    public int Priority => _onAccountPaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    public Task<PaymentTransaction> InitPaymentTransaction()
    {
        return Task.FromResult<PaymentTransaction>(null);
    }

    public async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        var result = new ProcessPaymentResult {
            NewPaymentTransactionStatus = TransactionStatus.Pending
        };

        var customer = await _customerService.GetCustomerById(paymentTransaction.CustomerId);
        var credit = customer != null ? await _companyCreditService.GetCompanyCredit(customer) : null;
        //the transaction amount is in the order's currency, the credit limit in the primary store currency
        var rate = paymentTransaction.CurrencyRate > 0 ? paymentTransaction.CurrencyRate : 1;
        if (credit == null ||
            !credit.Covers(paymentTransaction.TransactionAmount / rate, _onAccountPaymentSettings.BlockWhenOverdue))
        {
            result.AddError(_translationService.GetResource("Plugins.Payments.OnAccount.CreditExceeded"));
            return result;
        }

        //the due date is a calendar date, counted from the day the order is placed
        result.PaymentDueDateUtc = DateTime.UtcNow.Date.AddDays(credit.PaymentTermDays!.Value);
        return result;
    }

    public Task PostProcessPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.CompletedTask;
    }

    public Task<string> PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        //nothing
        return Task.FromResult(string.Empty);
    }

    public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
    {
        var workContext = _contextAccessor.WorkContext;
        var credit = await _companyCreditService.GetCompanyCredit(workContext.CurrentCustomer);
        if (!credit.Enabled)
            return true;

        //the subtotal (with discounts and tax) decides here: a payment provider cannot depend on the order
        //total, which is calculated through the payment service. Shipping and fees are not in it, so the exact
        //amount is checked again when the order is placed
        var subtotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart,
            new CalculationContext(workContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore,
                workContext.WorkingCurrency, workContext.TaxDisplayType), true);
        var amount = await _currencyService.ConvertToPrimaryStoreCurrency(subtotal.SubTotalWithDiscount,
            workContext.WorkingCurrency);

        return !credit.Covers(amount, _onAccountPaymentSettings.BlockWhenOverdue);
    }

    public Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
    {
        return Task.FromResult(0d);
    }

    public Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model)
    {
        return Task.FromResult<PaymentTransaction>(null);
    }

    public Task<string> GetControllerRouteName()
    {
        //the payment info step is always skipped (see SkipPaymentInfo), so there is no payment info page
        return Task.FromResult(string.Empty);
    }

    public Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
    {
        var result = new CapturePaymentResult();
        result.AddError("Capture method not supported");
        return Task.FromResult(result);
    }

    public Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        //it's not a redirection payment method
        return Task.FromResult(false);
    }

    public Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();
        result.AddError("Refund method not supported");
        return Task.FromResult(result);
    }

    public Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
    {
        var result = new VoidPaymentResult();
        result.AddError("Void method not supported");
        return Task.FromResult(result);
    }

    public async Task CancelPayment(PaymentTransaction paymentTransaction)
    {
        paymentTransaction.TransactionStatus = TransactionStatus.Canceled;
        await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
    }

    public Task<bool> SupportCapture() => Task.FromResult(false);

    public Task<bool> SupportPartiallyRefund() => Task.FromResult(false);

    public Task<bool> SupportRefund() => Task.FromResult(false);

    public Task<bool> SupportVoid() => Task.FromResult(false);

    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    public PurchaseOrderNumberMode PurchaseOrderNumberMode => _onAccountPaymentSettings.PurchaseOrderNumberMode;

    public Task<bool> SkipPaymentInfo() => Task.FromResult(true);

    public Task<string> Description()
    {
        //the available credit is not printed: it changes with every order and would be stale here
        return Task.FromResult(_translationService.GetResource("Plugins.Payments.OnAccount.Description"));
    }

    public string LogoURL => "/Plugins/Payments.OnAccount/logo.svg";
}
