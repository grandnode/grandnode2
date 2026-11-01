using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The order being placed, as an order placement rule sees it
/// </summary>
public class OrderPlacementContext
{
    private readonly Func<Task<double?>> _getTotal;
    private Task<double?> _total;

    public OrderPlacementContext(IList<ShoppingCartItem> cart, CalculationContext calculation, Language language,
        string paymentMethodSystemName, Func<Task<double?>> getTotal)
    {
        Cart = cart;
        Calculation = calculation;
        Language = language;
        PaymentMethodSystemName = paymentMethodSystemName;
        _getTotal = getTotal;
    }

    /// <summary>
    ///     Gets the shopping cart
    /// </summary>
    public IList<ShoppingCartItem> Cart { get; }

    /// <summary>
    ///     Gets the customer, store, currency and tax display the cart is calculated for
    /// </summary>
    public CalculationContext Calculation { get; }

    /// <summary>
    ///     Gets the language to write the reason in
    /// </summary>
    public Language Language { get; }

    /// <summary>
    ///     Gets the chosen payment method; null until the buyer has chosen one
    /// </summary>
    public string PaymentMethodSystemName { get; }

    /// <summary>
    ///     Gets the order total in the primary store currency; null until shipping is chosen.
    ///     It is calculated once, on first use, however many rules ask for it.
    /// </summary>
    public Task<double?> GetTotal()
    {
        return _total ??= _getTotal();
    }
}
