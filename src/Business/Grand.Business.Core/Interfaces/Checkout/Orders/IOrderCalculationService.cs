using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Order service interface
/// </summary>
public interface IOrderCalculationService
{
    /// <summary>
    ///     Gets shopping cart subtotal
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    Task<ShoppingCartSubTotal> GetShoppingCartSubTotal(IList<ShoppingCartItem> cart, CalculationContext context,
        bool includingTax);

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


    /// <summary>
    ///     Gets tax
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="usePaymentMethodAdditionalFee">
    ///     A value indicating whether we should use payment method additional fee when
    ///     calculating tax
    /// </param>
    /// <returns>Tax total</returns>
    Task<ShoppingCartTaxTotal> GetTaxTotal(IList<ShoppingCartItem> cart,
        CalculationContext context, bool usePaymentMethodAdditionalFee = true);

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

    /// <summary>
    ///     Converts existing loyalty points to amount
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points</param>
    /// <returns>Converted value in the primary store currency</returns>
    Task<double> ConvertLoyaltyPointsToAmount(int loyaltyPoints);

    /// <summary>
    ///     Converts an amount to loyalty points
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <returns>Converted value</returns>
    int ConvertAmountToLoyaltyPoints(double amount);

    /// <summary>
    ///     Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points to check</param>
    /// <returns>true - loyalty points could use; false - cannot be used.</returns>
    bool CheckMinimumLoyaltyPointsToUseRequirement(int loyaltyPoints);
}