using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart;

public class ShoppingCartItemWarningsEvent<C, S, P> : INotification
    where C : Customer where S : ShoppingCartItem where P : Product
{
    public ShoppingCartItemWarningsEvent(IList<string> warnings, C customer, S shoppingCartItem, P product,
        Language language)
    {
        Warnings = warnings;
        Customer = customer;
        ShoppingCartItem = shoppingCartItem;
        Product = product;
        Language = language;
    }

    public IList<string> Warnings { get; }

    public C Customer { get; }

    public S ShoppingCartItem { get; }

    public P Product { get; }

    /// <summary>
    ///     Language the warnings are written in
    /// </summary>
    public Language Language { get; }
}