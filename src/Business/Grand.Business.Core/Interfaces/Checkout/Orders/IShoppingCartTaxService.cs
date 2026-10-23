using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Calculates the tax of a shopping cart: subtotal, shipping and payment method additional fee
/// </summary>
public interface IShoppingCartTaxService
{
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
}
