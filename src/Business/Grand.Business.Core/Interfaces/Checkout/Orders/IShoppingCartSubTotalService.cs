using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Calculates the shopping cart subtotal.
///     Kept apart from <see cref="IOrderCalculationService" /> so a payment provider can depend on it:
///     the order calculation depends on the payment service, which depends on every payment provider.
/// </summary>
public interface IShoppingCartSubTotalService
{
    /// <summary>
    ///     Gets shopping cart subtotal
    /// </summary>
    /// <param name="cart">Cart</param>
    /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
    Task<(double discountAmount, List<ApplyDiscount> appliedDiscounts, double subTotalWithoutDiscount, double
            subTotalWithDiscount, SortedDictionary<double, double> taxRates)>
        GetShoppingCartSubTotal(IList<ShoppingCartItem> cart, bool includingTax);
}
