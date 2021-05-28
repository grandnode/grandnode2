using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Extensions
{
    /// <summary>
    /// Payment extensions
    /// </summary>
    public static class PaymentExtensions
    {
        /// <summary>
        /// Is payment method active?
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="paymentSettings">Payment settings</param>
        /// <returns>Result</returns>
        public static bool IsPaymentMethodActive(this IPaymentProvider paymentMethod,
            PaymentSettings paymentSettings)
        {
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));

            if (paymentSettings == null)
                throw new ArgumentNullException(nameof(paymentSettings));

            if (paymentSettings.ActivePaymentProviderSystemNames == null)
                return false;
            foreach (var activeMethodSystemName in paymentSettings.ActivePaymentProviderSystemNames)
                if (paymentMethod.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// Calculate payment method fee
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
            if (paymentMethod == null)
                throw new ArgumentNullException(nameof(paymentMethod));
            if (fee <= 0)
                return fee;

            double result;
            if (usePercentage)
            {
                //percentage
                var shoppingCartSubTotal = await orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
                result = (double)((((float)shoppingCartSubTotal.subTotalWithDiscount) * ((float)fee)) / 100f);
            }
            else
            {
                //fixed value
                result = fee;
            }
            return result;
        }

    }
}
