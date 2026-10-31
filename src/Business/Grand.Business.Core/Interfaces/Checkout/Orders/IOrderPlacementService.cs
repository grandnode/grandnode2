using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Asks the order placement rules about the order being placed
/// </summary>
public interface IOrderPlacementService
{
    /// <summary>
    ///     Runs every rule that applies to the store and the customer and combines their answers
    /// </summary>
    /// <param name="cart">Shopping cart of the customer</param>
    /// <param name="calculation">Customer, store and currency the cart is calculated for</param>
    /// <param name="language">Language the reasons are written in</param>
    /// <param name="paymentMethodSystemName">Chosen payment method; null until the buyer has chosen one</param>
    /// <param name="cancellationToken">Cancellation token, passed on to every rule</param>
    Task<OrderPlacementEvaluation> Evaluate(IList<ShoppingCartItem> cart, CalculationContext calculation,
        Language language, string paymentMethodSystemName, CancellationToken cancellationToken = default);
}
