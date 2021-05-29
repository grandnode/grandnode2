using Braintree;
using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Payments.BrainTree.Models;
using Payments.BrainTree.Validators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Payments.BrainTree
{
    public class BrainTreePaymentProvider : IPaymentProvider
    {
        private readonly ITranslationService _translationService;
        private readonly ICustomerService _customerService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        public BrainTreePaymentProvider(
            ITranslationService translationService,
            ICustomerService customerService,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            BrainTreePaymentSettings brainTreePaymentSettings)
        {
            _translationService = translationService;
            _customerService = customerService;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
            _brainTreePaymentSettings = brainTreePaymentSettings;
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentBrainTree";
        }
        /// <summary>
        /// Init a process a payment transaction
        /// </summary>
        /// <returns>Payment transaction</returns>
        public async Task<PaymentTransaction> InitPaymentTransaction()
        {
            return await Task.FromResult<PaymentTransaction>(null);
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_brainTreePaymentSettings, _translationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"],
                CardNonce = form["CardNonce"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                {
                    warnings.Add(error.ErrorMessage);
                }
            return await Task.FromResult(warnings);
        }

        public async Task<PaymentTransaction> SavePaymentInfo(IFormCollection form)
        {

            if (form.TryGetValue("CardNonce", out var cardNonce) && !StringValues.IsNullOrEmpty(cardNonce))
                _httpContextAccessor.HttpContext.Session.SetString("CardNonce", cardNonce.ToString());
                
            if (form.TryGetValue("CardholderName", out var cardholderName) && !StringValues.IsNullOrEmpty(cardholderName))
                _httpContextAccessor.HttpContext.Session.SetString("CardholderName", cardholderName.ToString());

            if (form.TryGetValue("CardNumber", out var cardNumber) && !StringValues.IsNullOrEmpty(cardNumber))
                _httpContextAccessor.HttpContext.Session.SetString("CardNumber", cardNumber.ToString());

            if (form.TryGetValue("ExpireMonth", out var expireMonth) && !StringValues.IsNullOrEmpty(expireMonth))
                _httpContextAccessor.HttpContext.Session.SetString("ExpireMonth", expireMonth.ToString());

            if (form.TryGetValue("ExpireYear", out var expireYear) && !StringValues.IsNullOrEmpty(expireYear))
                _httpContextAccessor.HttpContext.Session.SetString("ExpireYear", expireYear.ToString());

            if (form.TryGetValue("CardCode", out var creditCardCvv2) && !StringValues.IsNullOrEmpty(creditCardCvv2))
                _httpContextAccessor.HttpContext.Session.SetString("CardCode", creditCardCvv2.ToString());

            return await Task.FromResult<PaymentTransaction>(null);
        }

        /// <summary>
        /// Process a payment
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
            var gateway = new BraintreeGateway
            {
                Environment = useSandBox ? Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = merchantId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            //new transaction request
            var transactionRequest = new TransactionRequest
            {
                Amount = Convert.ToDecimal(paymentTransaction.TransactionAmount),
            };

            if (_brainTreePaymentSettings.Use3DS)
            {
                transactionRequest.PaymentMethodNonce = _httpContextAccessor.HttpContext.Session.GetString("CardNonce").ToString();
            }
            else
            {
                //transaction credit card request
                var transactionCreditCardRequest = new TransactionCreditCardRequest
                {
                    Number = _httpContextAccessor.HttpContext.Session.GetString("CardNumber").ToString(),
                    CVV = _httpContextAccessor.HttpContext.Session.GetString("CardCode").ToString(),
                    ExpirationDate = _httpContextAccessor.HttpContext.Session.GetString("ExpireMonth").ToString() + "/" + _httpContextAccessor.HttpContext.Session.GetString("ExpireYear").ToString()
                };
                transactionRequest.CreditCard = transactionCreditCardRequest;
            }

            //address request
            var addressRequest = new AddressRequest
            {
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                StreetAddress = customer.BillingAddress.Address1,
                PostalCode = customer.BillingAddress.ZipPostalCode
            };
            transactionRequest.BillingAddress = addressRequest;

            //transaction options request
            var transactionOptionsRequest = new TransactionOptionsRequest
            {
                SubmitForSettlement = true
            };
            transactionRequest.Options = transactionOptionsRequest;

            //sending a request
            var result = gateway.Transaction.Sale(transactionRequest);

            //result
            if (result.IsSuccess())
            {
                processPaymentResult.PaidAmount = paymentTransaction.TransactionAmount;
                processPaymentResult.NewPaymentTransactionStatus = Grand.Domain.Payments.TransactionStatus.Paid;
            }
            else
            {
                processPaymentResult.AddError("Error processing payment." + result.Message);
            }

            return processPaymentResult;
        }

        /// <summary>
        /// Post process payment 
        /// </summary>
        public Task PostProcessPayment(PaymentTransaction paymentTransaction)
        {
            //nothing
            return Task.CompletedTask;
        }

        /// <summary>
        /// Post redirect payment (used by payment gateways that redirecting to a another URL)
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        public Task PostRedirectPayment(PaymentTransaction paymentTransaction)
        {    
            //nothing
            return Task.CompletedTask;
        }
        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
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
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            if (_brainTreePaymentSettings.AdditionalFee <= 0)
                return _brainTreePaymentSettings.AdditionalFee;

            double result;
            if (_brainTreePaymentSettings.AdditionalFeePercentage)
            {
                //percentage
                var orderTotalCalculationService = _serviceProvider.GetRequiredService<IOrderCalculationService>();
                var subtotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
                result = (double)((((float)subtotal.subTotalWithDiscount) * ((float)_brainTreePaymentSettings.AdditionalFee)) / 100f);
            }
            else
            {
                //fixed value
                result = _brainTreePaymentSettings.AdditionalFee;
            }
            if (result > 0)
            {
                var currencyService = _serviceProvider.GetRequiredService<ICurrencyService>();
                var workContext = _serviceProvider.GetRequiredService<IWorkContext>();
                result = await currencyService.ConvertFromPrimaryStoreCurrency(result, workContext.WorkingCurrency);
            }
            //return result;
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <returns>Capture payment result</returns>
        public async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Refunds a payment
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
        /// Voids a payment
        /// </summary>
        /// <returns>Result</returns>
        public async Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Cancel a payment
        /// </summary>
        /// <returns>Result</returns>
        public Task CancelPayment(PaymentTransaction paymentTransaction)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            //it's not a redirection payment method. So we always return false
            return await Task.FromResult(false);
        }



        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public async Task<bool> SupportCapture()
        {
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public async Task<bool> SupportPartiallyRefund()
        {
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public async Task<bool> SupportRefund()
        {
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public async Task<bool> SupportVoid()
        {
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        public string ConfigurationUrl => BrainTreeDefaults.ConfigurationUrl;

        public string SystemName => BrainTreeDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(BrainTreeDefaults.FriendlyName);

        public int Priority => _brainTreePaymentSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public async Task<bool> SkipPaymentInfo()
        {
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public async Task<string> Description()
        {
            return await Task.FromResult(_translationService.GetResource("Plugins.Payments.BrainTree.PaymentMethodDescription"));
        }

        public string LogoURL => "/Plugins/Payments.BrainTree/logo.jpg";

    }
}
