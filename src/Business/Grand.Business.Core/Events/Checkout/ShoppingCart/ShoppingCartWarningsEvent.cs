using Grand.Domain.Common;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart;

public class ShoppingCartWarningsEvent<T, U> : INotification where U : ShoppingCartItem
{
    public ShoppingCartWarningsEvent(IList<T> warnings, IList<U> shoppingCartItems,
        IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes)
    {
        Warnings = warnings;
        ShoppingCartItems = shoppingCartItems;
        CheckoutAttributes = checkoutAttributes;
        ValidateCheckoutAttributes = validateCheckoutAttributes;
    }

    public IList<T> Warnings { get; }

    public IList<U> ShoppingCartItems { get; }

    public IList<CustomAttribute> CheckoutAttributes { get; }

    public bool ValidateCheckoutAttributes { get; }
}