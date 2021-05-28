using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Payments.PayPalStandard
{
    public class PayPalStandardPaymentProvider : IPaymentProvider
    {

        private readonly ITranslationService _translationService;
        private readonly PayPalStandardPaymentSettings _paypalStandardPaymentSettings;

        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IUserFieldService _userFieldService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITaxService _taxService;
        private readonly IProductService _productService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;

        #region Ctor

        public PayPalStandardPaymentProvider(
            ICheckoutAttributeParser checkoutAttributeParser,
            IUserFieldService userFieldService,
            IHttpContextAccessor httpContextAccessor,
            ITranslationService translationService,
            ITaxService taxService,
            IProductService productService,
            IServiceProvider serviceProvider,
            IWorkContext workContext,
            IOrderService orderService,
            PayPalStandardPaymentSettings paypalStandardPaymentSettings)
        {
            _checkoutAttributeParser = checkoutAttributeParser;
            _userFieldService = userFieldService;
            _httpContextAccessor = httpContextAccessor;
            _translationService = translationService;
            _taxService = taxService;
            _productService = productService;
            _serviceProvider = serviceProvider;
            _workContext = workContext;
            _orderService = orderService;
            _paypalStandardPaymentSettings = paypalStandardPaymentSettings;
        }

        #endregion

        public string ConfigurationUrl => PayPalStandardPaymentDefaults.ConfigurationUrl;

        public string SystemName => PayPalStandardPaymentDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(PayPalStandardPaymentDefaults.FriendlyName);

        public int Priority => _paypalStandardPaymentSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();


        #region Utilities

        /// <summary>
        /// Gets PayPal URL
        /// </summary>
        /// <returns></returns>
        private string GetPaypalUrl()
        {
            return _paypalStandardPaymentSettings.UseSandbox ?
                "https://www.sandbox.paypal.com/us/cgi-bin/webscr" :
                "https://www.paypal.com/us/cgi-bin/webscr";
        }

        /// <summary>
        /// Gets IPN PayPal URL
        /// </summary>
        /// <returns></returns>
        private string GetIpnPaypalUrl()
        {
            return _paypalStandardPaymentSettings.UseSandbox ?
                "https://ipnpb.sandbox.paypal.com/cgi-bin/webscr" :
                "https://ipnpb.paypal.com/cgi-bin/webscr";
        }

        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        public bool GetPdtDetails(string tx, out Dictionary<string, string> values, out string response)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetPaypalUrl());
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = "application/x-www-form-urlencoded";
            //now PayPal requires user-agent. otherwise, we can get 403 error
            req.UserAgent = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];

            var formContent = $"cmd=_notify-synch&at={_paypalStandardPaymentSettings.PdtToken}&tx={tx}";
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                sw.Write(formContent);

            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
                response = WebUtility.UrlDecode(sr.ReadToEnd());

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true, success = false;
            foreach (var l in response.Split('\n'))
            {
                var line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    var equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
                }
            }

            return success;
        }

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        public bool VerifyIpn(string formString, out Dictionary<string, string> values)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetIpnPaypalUrl());
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = "application/x-www-form-urlencoded";
            //now PayPal requires user-agent. otherwise, we can get 403 error
            req.UserAgent = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];

            var formContent = $"cmd=_notify-validate&{formString}";
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = WebUtility.UrlDecode(sr.ReadToEnd());
            }
            var success = response.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in formString.Split('&'))
            {
                var line = l.Trim();
                var equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }

            return success;
        }

        /// <summary>
        /// Create common query parameters for the request
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Created query parameters</returns>
        private async Task<IDictionary<string, string>> CreateQueryParameters(Order order)
        {
            //get store location
            var storeLocation = _workContext.CurrentStore.SslEnabled ? _workContext.CurrentStore.SecureUrl.TrimEnd('/') : _workContext.CurrentStore.Url.TrimEnd('/');
            var stateProvince = "";
            var countryCode = "";
            if (!String.IsNullOrEmpty(order.ShippingAddress?.StateProvinceId))
            {
                var countryService = _serviceProvider.GetRequiredService<ICountryService>();
                var country = await countryService.GetCountryById(order.ShippingAddress?.CountryId);
                var state = country?.StateProvinces.FirstOrDefault(x => x.Id == order.ShippingAddress?.StateProvinceId);
                if (state != null)
                    stateProvince = state.Abbreviation;
            }
            if (!String.IsNullOrEmpty(order.ShippingAddress?.CountryId))
            {
                var country = await _serviceProvider.GetRequiredService<ICountryService>().GetCountryById(order.ShippingAddress?.CountryId);
                if (country != null)
                    countryCode = country.TwoLetterIsoCode;
            }


            //create query parameters
            return new Dictionary<string, string>
            {
                //PayPal ID or an email address associated with your PayPal account
                ["business"] = _paypalStandardPaymentSettings.BusinessEmail,

                //the character set and character encoding
                ["charset"] = "utf-8",

                //set return method to "2" (the customer redirected to the return URL by using the POST method, and all payment variables are included)
                ["rm"] = "2",

                ["currency_code"] = order.CustomerCurrencyCode,

                //order identifier
                ["invoice"] = order.OrderNumber.ToString(),
                ["custom"] = order.OrderGuid.ToString(),

                //PDT, IPN and cancel URL
                ["return"] = $"{storeLocation}/Plugins/PaymentPayPalStandard/PDTHandler",
                ["notify_url"] = $"{storeLocation}/Plugins/PaymentPayPalStandard/IPNHandler",
                ["cancel_return"] = $"{storeLocation}/Plugins/PaymentPayPalStandard/CancelOrder",

                //shipping address, if exists
                ["no_shipping"] = order.ShippingStatusId == ShippingStatus.ShippingNotRequired ? "1" : "2",
                ["address_override"] = order.ShippingStatusId == ShippingStatus.ShippingNotRequired ? "0" : "1",
                ["first_name"] = order.ShippingAddress?.FirstName,
                ["last_name"] = order.ShippingAddress?.LastName,
                ["address1"] = order.ShippingAddress?.Address1,
                ["address2"] = order.ShippingAddress?.Address2,
                ["city"] = order.ShippingAddress?.City,

                ["state"] = stateProvince,
                ["country"] = countryCode,
                ["zip"] = order.ShippingAddress?.ZipPostalCode,
                ["email"] = order.ShippingAddress?.Email
            };
        }

        /// <summary>
        /// Add order items to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        private async Task AddItemsParameters(IDictionary<string, string> parameters, Order order)
        {
            //upload order items
            parameters.Add("cmd", "_cart");
            parameters.Add("upload", "1");

            double cartTotal = 0;
            double roundedCartTotal = 0;
            var itemCount = 1;

            //add shopping cart items
            foreach (var item in order.OrderItems)
            {
                var product = await _productService.GetProductById(item.ProductId);

                var roundedItemPrice = Math.Round(item.UnitPriceExclTax, 2);

                //add query parameters
                parameters.Add($"item_name_{itemCount}", product.Name);
                parameters.Add($"amount_{itemCount}", roundedItemPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", item.Quantity.ToString());

                cartTotal += (item.PriceExclTax);
                roundedCartTotal += roundedItemPrice * item.Quantity;
                itemCount++;
            }

            //add checkout attributes as order items
            var checkoutAttributeValues = await _checkoutAttributeParser.ParseCheckoutAttributeValue(order.CheckoutAttributes);
            var currencyService = _serviceProvider.GetRequiredService<ICurrencyService>();
            var workContext = _serviceProvider.GetRequiredService<IWorkContext>();
            var customer = await _serviceProvider.GetRequiredService<ICustomerService>().GetCustomerById(order.CustomerId);
            foreach (var attributeValue in checkoutAttributeValues)
            {
                var attributePrice = await _taxService.GetCheckoutAttributePrice(attributeValue.ca, attributeValue.cav, false, customer);
                if (attributePrice.checkoutPrice > 0)
                {
                    double roundedAttributePrice = Math.Round(await currencyService.ConvertFromPrimaryStoreCurrency(attributePrice.checkoutPrice, workContext.WorkingCurrency), 2);
                    //add query parameters
                    if (attributeValue.ca != null)
                    {
                        parameters.Add($"item_name_{itemCount}", attributeValue.ca.Name);
                        parameters.Add($"amount_{itemCount}", roundedAttributePrice.ToString("0.00", CultureInfo.InvariantCulture));
                        parameters.Add($"quantity_{itemCount}", "1");

                        cartTotal += attributePrice.checkoutPrice;
                        roundedCartTotal += roundedAttributePrice;
                        itemCount++;
                    }
                }
            }

            //add shipping fee as a separate order item, if it has price
            var roundedShippingPrice = Math.Round(order.OrderShippingExclTax, 2);
            if (roundedShippingPrice > 0)
            {
                parameters.Add($"item_name_{itemCount}", "Shipping fee");
                parameters.Add($"amount_{itemCount}", roundedShippingPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += (order.OrderShippingExclTax);
                roundedCartTotal += roundedShippingPrice;
                itemCount++;
            }

            //add payment method additional fee as a separate order item, if it has price
            var roundedPaymentMethodPrice = Math.Round(order.PaymentMethodAdditionalFeeExclTax, 2);
            if (roundedPaymentMethodPrice > 0)
            {
                parameters.Add($"item_name_{itemCount}", "Payment method fee");
                parameters.Add($"amount_{itemCount}", roundedPaymentMethodPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += (order.PaymentMethodAdditionalFeeExclTax);
                roundedCartTotal += roundedPaymentMethodPrice;
                itemCount++;
            }

            //add tax as a separate order item, if it has positive amount
            var roundedTaxAmount = Math.Round(order.OrderTax, 2);
            if (roundedTaxAmount > 0)
            {
                parameters.Add($"item_name_{itemCount}", "Tax amount");
                parameters.Add($"amount_{itemCount}", roundedTaxAmount.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += (order.OrderTax);
                roundedCartTotal += roundedTaxAmount;
                itemCount++;
            }

            if (cartTotal > order.OrderTotal)
            {
                //get the difference between what the order total is and what it should be and use that as the "discount"
                var discountTotal = Math.Round(cartTotal - (order.OrderTotal), 2);
                roundedCartTotal -= discountTotal;

                //gift voucher or loyalty points amount applied to cart in nopCommerce - shows in PayPal as "discount"
                parameters.Add("discount_amount_cart", discountTotal.ToString("0.00", CultureInfo.InvariantCulture));
            }

            //save order total that actually sent to PayPal (used for PDT order total validation)
            await _userFieldService.SaveField(order, PaypalHelper.OrderTotalSentToPayPal, roundedCartTotal);
        }

        /// <summary>
        /// Add order total to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        private async Task AddOrderTotalParameters(IDictionary<string, string> parameters, Order order)
        {
            //round order total
            var roundedOrderTotal = Math.Round(order.OrderTotal, 2);

            parameters.Add("cmd", "_xclick");
            parameters.Add("item_name", $"Order Number {order.OrderNumber}");
            parameters.Add("amount", roundedOrderTotal.ToString("0.00", CultureInfo.InvariantCulture));

            //save order total that actually sent to PayPal (used for PDT order total validation)
            await _userFieldService.SaveField(order, PaypalHelper.OrderTotalSentToPayPal, roundedOrderTotal);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Init a process a payment transaction
        /// </summary>
        /// <returns>Payment transaction</returns>
        public async Task<PaymentTransaction> InitPaymentTransaction()
        {
            return await Task.FromResult<PaymentTransaction>(null);
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
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
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public async Task PostRedirectPayment(PaymentTransaction paymentTransaction)
        {
            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            //create common query parameters for the request
            var queryParameters = await CreateQueryParameters(order);

            //whether to include order items in a transaction
            if (_paypalStandardPaymentSettings.PassProductNamesAndTotals)
            {
                //add order items query parameters to the request
                var parameters = new Dictionary<string, string>(queryParameters);
                await AddItemsParameters(parameters, order);

                //remove null values from parameters
                parameters = parameters.Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                    .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

                //ensure redirect URL doesn't exceed 2K chars to avoid "too long URL" exception
                var redirectUrl = QueryHelpers.AddQueryString(GetPaypalUrl(), parameters);
                if (redirectUrl.Length <= 2048)
                {
                    _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);
                    return;
                }
            }

            //or add only an order total query parameters to the request
            await AddOrderTotalParameters(queryParameters, order);

            //remove null values from parameters
            queryParameters = queryParameters.Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

            var url = QueryHelpers.AddQueryString(GetPaypalUrl(), queryParameters);
            _httpContextAccessor.HttpContext.Response.Redirect(url);
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
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
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            if (_paypalStandardPaymentSettings.AdditionalFee <= 0)
                return _paypalStandardPaymentSettings.AdditionalFee;

            double result;
            if (_paypalStandardPaymentSettings.AdditionalFeePercentage)
            {
                //percentage
                var orderTotalCalculationService = _serviceProvider.GetRequiredService<IOrderCalculationService>();
                var subtotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
                result = (double)((((float)subtotal.subTotalWithDiscount) * ((float)_paypalStandardPaymentSettings.AdditionalFee)) / 100f);
            }
            else
            {
                //fixed value
                result = _paypalStandardPaymentSettings.AdditionalFee;
            }
            if (result > 0)
            {
                var currencyService = _serviceProvider.GetRequiredService<ICurrencyService>();
                result = await currencyService.ConvertFromPrimaryStoreCurrency(result, _workContext.WorkingCurrency);
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
        /// <returns>Result</returns>
        public async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            //ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - paymentTransaction.CreatedOnUtc).TotalSeconds < 15)
                return false;

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            return await Task.FromResult(new List<string>());
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public async Task<PaymentTransaction> SavePaymentInfo(IFormCollection form)
        {
            return await Task.FromResult<PaymentTransaction>(null);
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
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

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
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            return await Task.FromResult(_translationService.GetResource("Plugins.Payments.PayPalStandard.PaymentMethodDescription"));
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentPayPalStandard";
        }

        public string LogoURL => "/Plugins/Payments.PayPalStandard/logo.jpg";

        #endregion
    }
}
