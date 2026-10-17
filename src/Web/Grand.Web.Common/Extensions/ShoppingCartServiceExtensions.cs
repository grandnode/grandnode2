using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Reads the shopping cart of the current request
/// </summary>
public static class ShoppingCartServiceExtensions
{
    /// <summary>
    ///     Gets the cart items of the current request: its customer in its store
    /// </summary>
    /// <param name="shoppingCartService">Shopping cart service</param>
    /// <param name="contextAccessor">Context of the current request</param>
    /// <param name="shoppingCartType">Shopping cart types; pass none to load all records</param>
    public static Task<IList<ShoppingCartItem>> GetCurrentCart(this IShoppingCartService shoppingCartService,
        IContextAccessor contextAccessor, params ShoppingCartType[] shoppingCartType)
    {
        return shoppingCartService.GetShoppingCart(contextAccessor.WorkContext.CurrentCustomer,
            contextAccessor.StoreContext.CurrentStore.Id, shoppingCartType);
    }
}
