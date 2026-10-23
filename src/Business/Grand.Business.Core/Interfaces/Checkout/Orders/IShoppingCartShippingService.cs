using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Calculates the shipping of a shopping cart: free shipping, additional charges, shipping discounts and the total
/// </summary>
public interface IShoppingCartShippingService
{
    /// <summary>
    ///     Adjust shipping rate (free shipping, additional charges, discounts)
    /// </summary>
    /// <param name="shippingRate">Shipping rate to adjust</param>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Adjusted shipping rate</returns>
    Task<ShippingRateAdjustment> AdjustShippingRate(double shippingRate,
        IList<ShoppingCartItem> cart, CalculationContext context);

    /// <summary>
    ///     Gets shopping cart additional shipping charge
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Additional shipping charge</returns>
    Task<double> GetShoppingCartAdditionalShippingCharge(IList<ShoppingCartItem> cart, CalculationContext context);

    /// <summary>
    ///     Gets a value indicating whether shipping is free
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>A value indicating whether shipping is free</returns>
    Task<bool> IsFreeShipping(IList<ShoppingCartItem> cart, CalculationContext context);

    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <returns>Shipping total</returns>
    Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart,
        CalculationContext context);

    /// <summary>
    ///     Gets shopping cart shipping total
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    /// <returns>Shipping total</returns>
    Task<ShoppingCartShippingTotal> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart,
        CalculationContext context, bool includingTax);
}
