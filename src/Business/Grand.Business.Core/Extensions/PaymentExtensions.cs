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

    /// <summary>
    ///     Calculate payment method fee
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="orderTotalCalculationService">Order total calculation service</param>
    /// <param name="cart">Shopping cart</param>
    /// <param name="fee">Fee value</param>
    /// <param name="usePercentage">Is fee amount specified as percentage or fixed value?</param>
    /// <returns>Result</returns>
    public static async Task<double> CalculateAdditionalFee(this IPaymentProvider paymentMethod,
        IOrderCalculationService orderTotalCalculationService, IList<ShoppingCartItem> cart,
        double fee, bool usePercentage)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);
        if (fee <= 0)
            return fee;

        double result;
        if (usePercentage)
        {
            //percentage
            var shoppingCartSubTotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
            result = (float)shoppingCartSubTotal.subTotalWithDiscount * (float)fee / 100f;
        }
        else
        {
            //fixed value
            result = fee;
        }

        return result;
    }
}