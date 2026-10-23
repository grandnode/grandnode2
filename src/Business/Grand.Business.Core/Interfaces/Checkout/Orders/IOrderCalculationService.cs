using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Calculates the shopping cart total. The parts it adds up have their own services:
///     <see cref="IShoppingCartSubTotalService" />, <see cref="IShoppingCartShippingService" />,
///     <see cref="IShoppingCartTaxService" /> and <see cref="ILoyaltyPointsCalculationService" />.
/// </summary>
public interface IOrderCalculationService
{
    /// <summary>
    ///     Gets shopping cart total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="useLoyaltyPoints">
    ///     A value indicating loyalty points should be used; null to detect current choice of the
    ///     customer
    /// </param>
    /// <param name="usePaymentMethodAdditionalFee">
    ///     A value indicating whether we should use payment method additional fee when
    ///     calculating order total
    /// </param>
    /// <returns>Shopping cart total;Null if shopping cart total couldn't be calculated now</returns>
    Task<ShoppingCartTotal> GetShoppingCartTotal(IList<ShoppingCartItem> cart, CalculationContext context,
        bool? useLoyaltyPoints = null, bool usePaymentMethodAdditionalFee = true);
}
