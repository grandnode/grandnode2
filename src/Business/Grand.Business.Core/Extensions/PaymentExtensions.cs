using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;

namespace Grand.Business.Core.Extensions;

/// <summary>
///     Payment extensions
/// </summary>
public static class PaymentExtensions
{
    /// <summary>
    ///     Is payment method active?
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="paymentSettings">Payment settings</param>
    /// <returns>Result</returns>
    public static bool IsPaymentMethodActive(this IPaymentProvider paymentMethod,
        PaymentSettings paymentSettings)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);
        ArgumentNullException.ThrowIfNull(paymentSettings);

        return paymentSettings.ActivePaymentProviderSystemNames != null &&
               paymentSettings.ActivePaymentProviderSystemNames.Any(activeMethodSystemName =>
                   paymentMethod.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the name of the order's payment method in the order's language, for text that does not follow
    ///     the current request (e-mails, PDF). Falls back to the friendly name when the order has no language,
    ///     and to the system name when the provider is missing or gives no name.
    /// </summary>
    /// <param name="paymentMethod">Payment method, or null when it is no longer installed</param>
    /// <param name="order">Order</param>
    /// <returns>Payment method name</returns>
    public static string GetPaymentMethodName(this IPaymentProvider paymentMethod, Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (paymentMethod == null)
            return order.PaymentMethodSystemName;

        var name = string.IsNullOrEmpty(order.CustomerLanguageId)
            ? paymentMethod.FriendlyName
            : paymentMethod.GetFriendlyName(order.CustomerLanguageId);

        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(order.CustomerLanguageId))
            name = paymentMethod.FriendlyName;

        return string.IsNullOrEmpty(name) ? order.PaymentMethodSystemName : name;
    }
}