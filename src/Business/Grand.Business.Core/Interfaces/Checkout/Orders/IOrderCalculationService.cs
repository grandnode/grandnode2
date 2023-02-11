using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders
{
    /// <summary>
    /// Order service interface
    /// </summary>
    public interface IOrderCalculationService
    {
        /// <summary>
        /// Gets shopping cart subtotal
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        Task<(double discountAmount, List<ApplyDiscount> appliedDiscounts, double subTotalWithoutDiscount, double subTotalWithDiscount, SortedDictionary<double, double> taxRates)>
            GetShoppingCartSubTotal(IList<ShoppingCartItem> cart,
            bool includingTax);

        /// <summary>
        /// Adjust shipping rate (free shipping, additional charges, discounts)
        /// </summary>
        /// <param name="shippingRate">Shipping rate to adjust</param>
        /// <param name="cart">Cart</param>
        /// <returns>Adjusted shipping rate</returns>
        Task<(double shippingRate, List<ApplyDiscount> appliedDiscounts)> AdjustShippingRate(double shippingRate, IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets shopping cart additional shipping charge
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>Additional shipping charge</returns>
        Task<double> GetShoppingCartAdditionalShippingCharge(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets a value indicating whether shipping is free
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>A value indicating whether shipping is free</returns>
        Task<bool> IsFreeShipping(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets shopping cart shipping total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>Shipping total</returns>
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets shopping cart shipping total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <returns>Shipping total</returns>
        Task<(double? shoppingCartShippingTotal, double taxRate, List<ApplyDiscount> appliedDiscounts)> GetShoppingCartShippingTotal(IList<ShoppingCartItem> cart, bool includingTax);


        /// <summary>
        /// Gets tax
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee when calculating tax</param>
        /// <returns>Tax total</returns>
        Task<(double taxtotal, SortedDictionary<double, double> taxRates)> GetTaxTotal(IList<ShoppingCartItem> cart, bool usePaymentMethodAdditionalFee = true);

        /// <summary>
        /// Gets shopping cart total
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="useLoyaltyPoints">A value indicating loyalty points should be used; null to detect current choice of the customer</param>
        /// <param name="usePaymentMethodAdditionalFee">A value indicating whether we should use payment method additional fee when calculating order total</param>
        /// <returns>Shopping cart total;Null if shopping cart total couldn't be calculated now</returns>
        Task<(double? shoppingCartTotal, double discountAmount, List<ApplyDiscount> appliedDiscounts, List<AppliedGiftVoucher> appliedGiftVouchers,
            int redeemedLoyaltyPoints, double redeemedLoyaltyPointsAmount)>
            GetShoppingCartTotal(IList<ShoppingCartItem> cart, bool? useLoyaltyPoints = null, bool usePaymentMethodAdditionalFee = true);

        /// <summary>
        /// Converts existing loyalty points to amount
        /// </summary>
        /// <param name="loyaltyPoints">Loyalty points</param>
        /// <returns>Converted value</returns>
        Task<double> ConvertLoyaltyPointsToAmount(int loyaltyPoints);

        /// <summary>
        /// Converts an amount to loyalty points
        /// </summary>
        /// <param name="amount">Amount</param>
        /// <returns>Converted value</returns>
        int ConvertAmountToLoyaltyPoints(double amount);

        /// <summary>
        /// Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
        /// </summary>
        /// <param name="loyaltyPoints">Loyalty points to check</param>
        /// <returns>true - loyalty points could use; false - cannot be used.</returns>
        bool CheckMinimumLoyaltyPointsToUseRequirement(int loyaltyPoints);

    }
}
