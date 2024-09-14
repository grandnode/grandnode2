namespace Grand.Domain.Orders;

/// <summary>
///     Represents a shopping cart
/// </summary>
public static class ShoppingCartExtensions
{
    /// <summary>
    ///     Indicates whether the shopping cart requires shipping
    /// </summary>
    /// <param name="shoppingCart">Shopping cart</param>
    /// <returns>True if the shopping cart requires shipping; otherwise, false.</returns>
    public static bool RequiresShipping(this IList<ShoppingCartItem> shoppingCart)
    {
        return shoppingCart.Any(shoppingCartItem => shoppingCartItem.IsShipEnabled);
    }

    public static IEnumerable<ShoppingCartItem> LimitPerStore(this IEnumerable<ShoppingCartItem> cart,
        bool cartsSharedBetweenStores, string storeId)
    {
        return cartsSharedBetweenStores ? cart : cart.Where(x => x.StoreId == storeId);
    }
}