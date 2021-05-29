using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Payments
{
    /// <summary>
    /// Payment service
    /// </summary>
    public partial class PaymentService : IPaymentService
    {
        #region Fields

        private readonly PaymentSettings _paymentSettings;
        private readonly IEnumerable<IPaymentProvider> _paymentProviders;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="paymentSettings">Payment settings</param>
        /// <param name="PaymentProviders">Payment providers</param>
        /// <param name="settingService">Setting service</param>
        public PaymentService(PaymentSettings paymentSettings,
            IEnumerable<IPaymentProvider> paymentProviders,
            ISettingService settingService)
        {
            _paymentSettings = paymentSettings;
            _paymentProviders = paymentProviders;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load active payment methods
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="filterByCountryId">Specified country</param>
        /// <returns>Payment methods</returns>
        public virtual async Task<IList<IPaymentProvider>> LoadActivePaymentMethods(Customer customer = null, string storeId = "", string filterByCountryId = "")
        {
            var pm = LoadAllPaymentMethods(customer, storeId, filterByCountryId)
                   .Where(provider => _paymentSettings.ActivePaymentProviderSystemNames.Contains(provider.SystemName, StringComparer.OrdinalIgnoreCase))
                   .ToList();

            if (customer != null)
            {
                var selectedShippingOption = customer.GetUserFieldFromEntity<ShippingOption>(
                       SystemCustomerFieldNames.SelectedShippingOption, storeId);

                if (selectedShippingOption != null)
                {
                    for (int i = pm.Count - 1; i >= 0; i--)
                    {
                        var restictedGroupIds = GetRestrictedShippingIds(pm[i]);
                        if (restictedGroupIds.Contains(selectedShippingOption.Name))
                        {
                            pm.Remove(pm[i]);
                        }
                    }
                }

            }
            return await Task.FromResult(pm);
        }

        /// <summary>
        /// Load payment provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found payment provider</returns>
        public virtual IPaymentProvider LoadPaymentMethodBySystemName(string systemName)
        {
            return _paymentProviders.FirstOrDefault(x => x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Load all payment providers
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="customer">Customer</param>
        /// <param name="filterByCountryId">Load records allowed only in a specified country; pass "" to load all records</param>
        /// <returns>Payment providers</returns>
        public virtual IList<IPaymentProvider> LoadAllPaymentMethods(Customer customer = null, string storeId = "", string filterByCountryId = "")
        {
            var paymentMethods = _paymentProviders
                .Where(x =>
                    x.IsAuthenticateStore(storeId) &&
                    x.IsAuthenticateGroup(customer))
                .OrderBy(x => x.Priority).ToList();
            if (String.IsNullOrEmpty(filterByCountryId))
                return paymentMethods;

            //filter by country
            var paymentMetodsByCountry = new List<IPaymentProvider>();
            foreach (var pm in paymentMethods)
            {
                var restictedCountryIds = GetRestrictedCountryIds(pm);
                if (!restictedCountryIds.Contains(filterByCountryId))
                {
                    paymentMetodsByCountry.Add(pm);
                }
            }
            return paymentMetodsByCountry;
        }

        /// <summary>
        /// Gets a list of coutnry identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>A list of country identifiers</returns>
        public virtual IList<string> GetRestrictedCountryIds(IPaymentProvider paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var settingKey = string.Format("PaymentMethodRestictions.{0}", paymentMethod.SystemName);
            var restictedCountryIds = _settingService.GetSettingByKey<PaymentRestictedSettings>(settingKey);
            if (restictedCountryIds == null)
                return new List<string>();
            return restictedCountryIds.Ids;
        }

        /// <summary>
        /// Gets a list of role identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>A list of role identifiers</returns>
        public virtual IList<string> GetRestrictedShippingIds(IPaymentProvider paymentMethod)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var settingKey = string.Format("PaymentMethodRestictionsShipping.{0}", paymentMethod.SystemName);
            var restictedShippingIds = _settingService.GetSettingByKey<PaymentRestictedSettings>(settingKey);
            if (restictedShippingIds == null)
                return new List<string>();
            return restictedShippingIds.Ids;
        }
        /// <summary>
        /// Saves a list of country identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="countryIds">A list of country identifiers</param>
        public virtual async Task SaveRestictedCountryIds(IPaymentProvider paymentMethod, List<string> countryIds)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var settingKey = string.Format("PaymentMethodRestictions.{0}", paymentMethod.SystemName);
            await _settingService.SetSetting(settingKey, new PaymentRestictedSettings() { Ids = countryIds });
        }

        /// <summary>
        /// Saves a list of role identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="countryIds">A list of country identifiers</param>
        public virtual async Task SaveRestictedGroupIds(IPaymentProvider paymentMethod, List<string> groupIds)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var settingKey = string.Format("PaymentMethodRestictionsGroup.{0}", paymentMethod.SystemName);
            await _settingService.SetSetting(settingKey, new PaymentRestictedSettings() { Ids = groupIds });
        }

        /// <summary>
        /// Saves a list of shipping identifiers in which a certain payment method is now allowed
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="shippingIds">A list of country identifiers</param>
        public virtual async Task SaveRestictedShippingIds(IPaymentProvider paymentMethod, List<string> shippingIds)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            var settingKey = string.Format("PaymentMethodRestictionsShipping.{0}", paymentMethod.SystemName);
            await _settingService.SetSetting(settingKey, new PaymentRestictedSettings() { Ids = shippingIds });
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public virtual async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            if (paymentTransaction.TransactionAmount == 0)
            {
                var result = new ProcessPaymentResult
                {
                    NewPaymentTransactionStatus = TransactionStatus.Paid 
                };
                return result;
            }

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            return await paymentMethod.ProcessPayment(paymentTransaction);
        }

        /// <summary>
        /// Post process payment 
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public virtual async Task PostProcessPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            if (paymentTransaction.TransactionStatus == TransactionStatus.Paid)
                return;

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            await paymentMethod.PostProcessPayment(paymentTransaction);
        }

        /// <summary>
        /// Post redirect payment (used by payment gateways that redirecting to a another URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public virtual async Task PostRedirectPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            //already paid or order.OrderTotal == 0
            if (paymentTransaction.TransactionStatus == TransactionStatus.Paid)
                return;

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            await paymentMethod.PostRedirectPayment(paymentTransaction);
        }

        /// <summary>
        /// Cancel payment transaction
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        public virtual async Task CancelPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            await paymentMethod.CancelPayment(paymentTransaction);

        }


        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public virtual async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            if (!_paymentSettings.AllowRePostingPayments)
                return false;

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                return false; //Payment method couldn't be loaded (for example, was uninstalled)

            if (paymentMethod.PaymentMethodType != PaymentMethodType.Redirection)
                return false;   //this option is available only for redirection payment methods

            if (paymentTransaction.TransactionStatus == TransactionStatus.Canceled)
                return false;  //do not allow for cancelled orders

            if (paymentTransaction.TransactionStatus != TransactionStatus.Pending)
                return false;  //payment transaction status should be Pending

            return await paymentMethod.CanRePostRedirectPayment(paymentTransaction);
        }



        /// <summary>
        /// Gets an additional handling fee of a payment method
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>Additional handling fee</returns>
        public virtual async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, string paymentMethodSystemName)
        {
            if (String.IsNullOrEmpty(paymentMethodSystemName))
                return 0;

            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return 0;

            double result = await paymentMethod.GetAdditionalHandlingFee(cart);
            if (result < 0)
                result = 0;

            return result;
        }



        /// <summary>
        /// Gets a value indicating whether capture is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether capture is supported</returns>
        public virtual async Task<bool> SupportCapture(string paymentMethodSystemName)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;
            return await paymentMethod.SupportCapture();
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="paymentTransaction">Payment transaction</param>
        /// <returns>Capture payment result</returns>
        public virtual async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            return await paymentMethod.Capture(paymentTransaction);
        }



        /// <summary>
        /// Gets a value indicating whether partial refund is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether partial refund is supported</returns>
        public virtual async Task<bool> SupportPartiallyRefund(string paymentMethodSystemName)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;
            return await paymentMethod.SupportPartiallyRefund();
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether refund is supported</returns>
        public virtual async Task<bool> SupportRefund(string paymentMethodSystemName)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;
            return await paymentMethod.SupportRefund();
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public virtual async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(refundPaymentRequest.PaymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            return await paymentMethod.Refund(refundPaymentRequest);
        }



        /// <summary>
        /// Gets a value indicating whether void is supported by payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A value indicating whether void is supported</returns>
        public virtual async Task<bool> SupportVoid(string paymentMethodSystemName)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;
            return await paymentMethod.SupportVoid();
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public virtual async Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new GrandException("Payment method couldn't be loaded");
            return await paymentMethod.Void(paymentTransaction);
        }


        /// <summary>
        /// Gets a payment method type
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>A payment method type</returns>
        public virtual PaymentMethodType GetPaymentMethodType(string paymentMethodSystemName)
        {
            var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                return PaymentMethodType.Other;
            return paymentMethod.PaymentMethodType;
        }

        #endregion
    }
}
