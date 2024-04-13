using Braintree;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Payments.BrainTree.Models;
using Payments.BrainTree.Validators;
using Environment = Braintree.Environment;
using TransactionStatus = Grand.Domain.Payments.TransactionStatus;

namespace Payments.BrainTree;

public class BrainTreePaymentProvider : IPaymentProvider
{
    private readonly BrainTreePaymentSettings _brainTreePaymentSettings;
    private readonly ICustomerService _customerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITranslationService _translationService;

    public BrainTreePaymentProvider(
        ITranslationService translationService,
        ICustomerService customerService,
        IHttpContextAccessor httpContextAccessor,
        BrainTreePaymentSettings brainTreePaymentSettings)
    {
        _translationService = translationService;
        _customerService = customerService;
        _httpContextAccessor = httpContextAccessor;
        _brainTreePaymentSettings = brainTreePaymentSettings;
    }

    public Task<string> GetControllerRouteName()
    {
        return Task.FromResult("Plugin.PaymentBrainTree");
    }

    /// <summary>
    ///     Init a process a payment transaction
    /// </summary>
    /// <returns>Payment transaction</returns>
    public async Task<PaymentTransaction> InitPaymentTransaction()
    {
        return await Task.FromResult<PaymentTransaction>(null);
    }

    public async Task<IList<string>> ValidatePaymentForm(IDictionary<string, string> model)
    {
        var warnings = new List<string>();
        //validate
        var validator = new PaymentInfoValidator(_brainTreePaymentSettings, _translationService);
        var paymentInfoModel = new PaymentInfoModel();
        if (model.TryGetValue("CardholderName", out var cardholderName))
            paymentInfoModel.CardholderName = cardholderName;
        if (model.TryGetValue("CardNumber", out var cardNumber))
            paymentInfoModel.CardNumber = cardNumber;
        if (model.TryGetValue("CardCode", out var cardCode))
            paymentInfoModel.CardCode = cardCode;
        if (model.TryGetValue("ExpireMonth", out var expireMonth))
            paymentInfoModel.ExpireMonth = expireMonth;
        if (model.TryGetValue("ExpireYear", out var expireYear))
            paymentInfoModel.ExpireYear = expireYear;
        if (model.TryGetValue("CardNonce", out var cardNonce))
            paymentInfoModel.CardNonce = cardNonce;

        var validationResult = await validator.ValidateAsync(paymentInfoModel);
        if (validationResult.IsValid) return await Task.FromResult(warnings);
        warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));
        return await Task.FromResult(warnings);
    }

    public async Task<PaymentTransaction> SavePaymentInfo(IDictionary<string, string> model)
    {
        if (model.TryGetValue("CardNonce", out var cardNonce) && !StringValues.IsNullOrEmpty(cardNonce))
            _httpContextAccessor.HttpContext!.Session.SetString("CardNonce", cardNonce);

        if (model.TryGetValue("CardholderName", out var cardholderName) &&
            !StringValues.IsNullOrEmpty(cardholderName))
            _httpContextAccessor.HttpContext!.Session.SetString("CardholderName", cardholderName);

        if (model.TryGetValue("CardNumber", out var cardNumber) && !StringValues.IsNullOrEmpty(cardNumber))
            _httpContextAccessor.HttpContext!.Session.SetString("CardNumber", cardNumber);

        if (model.TryGetValue("ExpireMonth", out var expireMonth) && !StringValues.IsNullOrEmpty(expireMonth))
            _httpContextAccessor.HttpContext!.Session.SetString("ExpireMonth", expireMonth);

        if (model.TryGetValue("ExpireYear", out var expireYear) && !StringValues.IsNullOrEmpty(expireYear))
            _httpContextAccessor.HttpContext!.Session.SetString("ExpireYear", expireYear);

        if (model.TryGetValue("CardCode", out var creditCardCvv2) && !StringValues.IsNullOrEmpty(creditCardCvv2))
            _httpContextAccessor.HttpContext!.Session.SetString("CardCode", creditCardCvv2);

        return await Task.FromResult<PaymentTransaction>(null);
    }

    /// <summary>
    ///     Process a payment
    /// </summary>
    /// <returns>Process payment result</returns>
    public async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        var processPaymentResult = new ProcessPaymentResult();

        //get customer
        var customer = await _customerService.GetCustomerById(paymentTransaction.CustomerId);

        //get settings
        var useSandBox = _brainTreePaymentSettings.UseSandBox;
        var merchantId = _brainTreePaymentSettings.MerchantId;
        var publicKey = _brainTreePaymentSettings.PublicKey;
        var privateKey = _brainTreePaymentSettings.PrivateKey;

        //new gateway
        var gateway = new BraintreeGateway {
            Environment = useSandBox ? Environment.SANDBOX : Environment.PRODUCTION,
            MerchantId = merchantId,
            PublicKey = publicKey,
            PrivateKey = privateKey
        };

        //new transaction request
        var transactionRequest = new TransactionRequest {
            Amount = Convert.ToDecimal(paymentTransaction.TransactionAmount)
        };

        if (_brainTreePaymentSettings.Use3DS)
        {
            transactionRequest.PaymentMethodNonce =
                _httpContextAccessor.HttpContext!.Session.GetString("CardNonce")!;
        }
        else
        {
            //transaction credit card request
            var transactionCreditCardRequest = new TransactionCreditCardRequest {
                Number = _httpContextAccessor.HttpContext!.Session.GetString("CardNumber")!,
                CVV = _httpContextAccessor.HttpContext.Session.GetString("CardCode")!,
                ExpirationDate = _httpContextAccessor.HttpContext.Session.GetString("ExpireMonth")! + "/" +
                                 _httpContextAccessor.HttpContext.Session.GetString("ExpireYear")!
            };
            transactionRequest.CreditCard = transactionCreditCardRequest;
        }

        //address request
        var addressRequest = new AddressRequest {
            FirstName = customer.BillingAddress.FirstName,
            LastName = customer.BillingAddress.LastName,
            StreetAddress = customer.BillingAddress.Address1,
            PostalCode = customer.BillingAddress.ZipPostalCode
        };
        transactionRequest.BillingAddress = addressRequest;

        //transaction options request
        var transactionOptionsRequest = new TransactionOptionsRequest {
            SubmitForSettlement = true
        };
        transactionRequest.Options = transactionOptionsRequest;

        //sending a request
        var result = await gateway.Transaction.SaleAsync(transactionRequest);

        //result
        if (result.IsSuccess())
        {
            processPaymentResult.PaidAmount = paymentTransaction.TransactionAmount;
            processPaymentResult.NewPaymentTransactionStatus = TransactionStatus.Paid;
        }
        else
        {
            processPaymentResult.AddError("Error processing payment." + result.Message);
        }

        return processPaymentResult;
    }

    /// <summary>
    ///     Post process payment
    /// </summary>
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
        //return false;
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>Additional handling fee</returns>
    public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
    {
        if (_brainTreePaymentSettings.AdditionalFee <= 0)
            return _brainTreePaymentSettings.AdditionalFee;

        double result;
        if (_brainTreePaymentSettings.AdditionalFeePercentage)
        {
            //percentage
            var orderTotalCalculationService = _httpContextAccessor.HttpContext!.RequestServices
                .GetRequiredService<IOrderCalculationService>();
            var subtotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
            result = (float)subtotal.subTotalWithDiscount *
                (float)_brainTreePaymentSettings.AdditionalFee / 100f;
        }
        else
        {
            //fixed value
            result = _brainTreePaymentSettings.AdditionalFee;
        }

        if (!(result > 0)) return await Task.FromResult(result);
        var currencyService = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<ICurrencyService>();
        var workContext = _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IWorkContext>();
        result = await currencyService.ConvertFromPrimaryStoreCurrency(result, workContext.WorkingCurrency);

        //return result;
        return await Task.FromResult(result);
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
        var result = new RefundPaymentResult();
        result.AddError("Refund method not supported");
        return await Task.FromResult(result);
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
    /// <param name="paymentTransaction"></param>
    /// <returns>Result</returns>
    public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        //it's not a redirection payment method. So we always return false
        return await Task.FromResult(false);
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
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a value indicating whether refund is supported
    /// </summary>
    public async Task<bool> SupportRefund()
    {
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a value indicating whether void is supported
    /// </summary>
    public async Task<bool> SupportVoid()
    {
        return await Task.FromResult(false);
    }

    /// <summary>
    ///     Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    public string ConfigurationUrl => BrainTreeDefaults.ConfigurationUrl;

    public string SystemName => BrainTreeDefaults.ProviderSystemName;

    public string FriendlyName => _translationService.GetResource(BrainTreeDefaults.FriendlyName);

    public int Priority => _brainTreePaymentSettings.DisplayOrder;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

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
        return await Task.FromResult(
            _translationService.GetResource("Plugins.Payments.BrainTree.PaymentMethodDescription"));
    }

    public string LogoURL => "/Plugins/Payments.BrainTree/logo.jpg";
}