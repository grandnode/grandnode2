using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;

namespace Grand.Business.Checkout.Services.Payments;

/// <summary>
///     Payment service
/// </summary>
public class PaymentService : IPaymentService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="paymentSettings">Payment settings</param>
    /// <param name="paymentProviders">Payment providers</param>
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

    #region Fields

    private readonly PaymentSettings _paymentSettings;
    private readonly IEnumerable<IPaymentProvider> _paymentProviders;
    private readonly ISettingService _settingService;

    #endregion

    #region Methods

    /// <summary>
    ///     Load active payment methods
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="filterByCountryId">Specified country</param>
    /// <returns>Payment methods</returns>
    public virtual async Task<IList<IPaymentProvider>> LoadActivePaymentMethods(Customer customer = null,
        string storeId = "", string filterByCountryId = "")
    {
        var pm = LoadAllPaymentMethods(customer, storeId, filterByCountryId)
            .Where(provider =>
                _paymentSettings.ActivePaymentProviderSystemNames.Contains(provider.SystemName,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (customer == null) return await Task.FromResult(pm);
        var selectedShippingOption = customer.GetUserFieldFromEntity<ShippingOption>(
            SystemCustomerFieldNames.SelectedShippingOption, storeId);

        if (selectedShippingOption == null) return await Task.FromResult(pm);
        for (var i = pm.Count - 1; i >= 0; i--)
        {
            var restrictedGroupIds = GetRestrictedShippingIds(pm[i]);
            if (restrictedGroupIds.Contains(selectedShippingOption.Name)) pm.Remove(pm[i]);
        }

        return await Task.FromResult(pm);
    }

    /// <summary>
    ///     Load payment provider by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>Found payment provider</returns>
    public virtual IPaymentProvider LoadPaymentMethodBySystemName(string systemName)
    {
        return _paymentProviders.FirstOrDefault(
            x => x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Load all payment providers
    /// </summary>
    /// <param name="storeId">Store ident</param>
    /// <param name="customer">Customer</param>
    /// <param name="filterByCountryId">Load records allowed only in a specified country; pass "" to load all records</param>
    /// <returns>Payment providers</returns>
    public virtual IList<IPaymentProvider> LoadAllPaymentMethods(Customer customer = null, string storeId = "",
        string filterByCountryId = "")
    {
        var paymentMethods = _paymentProviders
            .Where(x =>
                x.IsAuthenticateStore(storeId) &&
                x.IsAuthenticateGroup(customer))
            .OrderBy(x => x.Priority).ToList();
        return string.IsNullOrEmpty(filterByCountryId)
            ? paymentMethods
            :
            //filter by country
            (from pm in paymentMethods
                let restrictedCountryIds = GetRestrictedCountryIds(pm)
                where !restrictedCountryIds.Contains(filterByCountryId)
                select pm).ToList();
    }

    /// <summary>
    ///     Gets a list of country identifiers in which a certain payment method is now allowed
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <returns>A list of country identifiers</returns>
    public virtual IList<string> GetRestrictedCountryIds(IPaymentProvider paymentMethod)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var settingKey = $"PaymentMethodRestictions.{paymentMethod.SystemName}";
        var restrictedCountryIds = _settingService.GetSettingByKey<PaymentRestrictedSettings>(settingKey);
        return restrictedCountryIds == null ? new List<string>() : restrictedCountryIds.Ids;
    }

    /// <summary>
    ///     Gets a list of role identifiers in which a certain payment method is now allowed
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <returns>A list of role identifiers</returns>
    public virtual IList<string> GetRestrictedShippingIds(IPaymentProvider paymentMethod)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var settingKey = $"PaymentMethodRestictionsShipping.{paymentMethod.SystemName}";
        var restrictedShippingIds = _settingService.GetSettingByKey<PaymentRestrictedSettings>(settingKey);
        return restrictedShippingIds == null ? new List<string>() : restrictedShippingIds.Ids;
    }

    /// <summary>
    ///     Saves a list of country identifiers in which a certain payment method is now allowed
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="countryIds">A list of country identifiers</param>
    public virtual async Task SaveRestrictedCountryIds(IPaymentProvider paymentMethod, List<string> countryIds)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var settingKey = $"PaymentMethodRestictions.{paymentMethod.SystemName}";
        await _settingService.SetSetting(settingKey, new PaymentRestrictedSettings { Ids = countryIds });
    }

    /// <summary>
    ///     Saves a list of role identifiers in which a certain payment method is now allowed
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="groupIds">A list of group identifiers</param>
    public virtual async Task SaveRestrictedGroupIds(IPaymentProvider paymentMethod, List<string> groupIds)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var settingKey = $"PaymentMethodRestictionsGroup.{paymentMethod.SystemName}";
        await _settingService.SetSetting(settingKey, new PaymentRestrictedSettings { Ids = groupIds });
    }

    /// <summary>
    ///     Saves a list of shipping identifiers in which a certain payment method is now allowed
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="shippingIds">A list of country identifiers</param>
    public virtual async Task SaveRestrictedShippingIds(IPaymentProvider paymentMethod, List<string> shippingIds)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var settingKey = $"PaymentMethodRestictionsShipping.{paymentMethod.SystemName}";
        await _settingService.SetSetting(settingKey, new PaymentRestrictedSettings { Ids = shippingIds });
    }

    /// <summary>
    ///     Process a payment
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>Process payment result</returns>
    public virtual async Task<ProcessPaymentResult> ProcessPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        if (paymentTransaction.TransactionAmount == 0)
        {
            var result = new ProcessPaymentResult {
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
    ///     Post process payment
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    public virtual async Task PostProcessPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        if (paymentTransaction.TransactionStatus == TransactionStatus.Paid)
            return;

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        await paymentMethod.PostProcessPayment(paymentTransaction);
    }

    /// <summary>
    ///     Post redirect payment (used by payment gateways that redirecting to a another URL)
    /// </summary>
    /// <param name="paymentTransaction">Payment Transaction</param>
    public virtual async Task PostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        //already paid or order.OrderTotal == 0
        if (paymentTransaction.TransactionStatus == TransactionStatus.Paid)
            return;

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        await paymentMethod.PostRedirectPayment(paymentTransaction);
    }

    /// <summary>
    ///     Cancel payment transaction
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    public virtual async Task CancelPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        await paymentMethod.CancelPayment(paymentTransaction);
    }


    /// <summary>
    ///     Gets a value indicating whether customers can complete a payment after order is placed but not completed (for
    ///     redirection payment methods)
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>Result</returns>
    public virtual async Task<bool> CanRePostRedirectPayment(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        if (!_paymentSettings.AllowRePostingPayments)
            return false;

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod is not { PaymentMethodType: PaymentMethodType.Redirection })
            return false; //Payment method couldn't be loaded (for example, was uninstalled)

        if (paymentTransaction.TransactionStatus is TransactionStatus.Canceled or not TransactionStatus.Pending)
            return false; //do not allow for cancelled orders

        return await paymentMethod.CanRePostRedirectPayment(paymentTransaction);
    }


    /// <summary>
    ///     Gets an additional handling fee of a payment method
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <param name="paymentMethodSystemName">Payment method system name</param>
    /// <returns>Additional handling fee</returns>
    public virtual async Task<double> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart,
        string paymentMethodSystemName)
    {
        if (string.IsNullOrEmpty(paymentMethodSystemName))
            return 0;

        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        if (paymentMethod == null)
            return 0;

        var result = await paymentMethod.GetAdditionalHandlingFee(cart);
        if (result < 0)
            result = 0;

        return result;
    }


    /// <summary>
    ///     Gets a value indicating whether capture is supported by payment method
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
    ///     Captures payment
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>Capture payment result</returns>
    public virtual async Task<CapturePaymentResult> Capture(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        return await paymentMethod.Capture(paymentTransaction);
    }


    /// <summary>
    ///     Gets a value indicating whether partial refund is supported by payment method
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
    ///     Gets a value indicating whether refund is supported by payment method
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
    ///     Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public virtual async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
    {
        var paymentMethod =
            LoadPaymentMethodBySystemName(refundPaymentRequest.PaymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        return await paymentMethod.Refund(refundPaymentRequest);
    }


    /// <summary>
    ///     Gets a value indicating whether void is supported by payment method
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
    ///     Voids a payment
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>Result</returns>
    public virtual async Task<VoidPaymentResult> Void(PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var paymentMethod = LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
        if (paymentMethod == null)
            throw new GrandException("Payment method couldn't be loaded");
        return await paymentMethod.Void(paymentTransaction);
    }


    /// <summary>
    ///     Gets a payment method type
    /// </summary>
    /// <param name="paymentMethodSystemName">Payment method system name</param>
    /// <returns>A payment method type</returns>
    public virtual PaymentMethodType GetPaymentMethodType(string paymentMethodSystemName)
    {
        var paymentMethod = LoadPaymentMethodBySystemName(paymentMethodSystemName);
        return paymentMethod?.PaymentMethodType ?? PaymentMethodType.Other;
    }

    #endregion
}