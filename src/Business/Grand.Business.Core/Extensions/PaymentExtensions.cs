using Grand.Business.Core.Interfaces.Checkout.Orders;
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
}