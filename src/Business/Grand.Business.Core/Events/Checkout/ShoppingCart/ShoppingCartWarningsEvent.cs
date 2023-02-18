using Grand.Domain.Common;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart
{
    public class ShoppingCartWarningsEvent<T, U> : INotification where U : ShoppingCartItem
    {
        private readonly IList<T> _warnings;
        private readonly IList<U> _shoppingCartItems;
        private readonly IList<CustomAttribute> _checkoutAttributes;
        private readonly bool _validateCheckoutAttributes;

        public ShoppingCartWarningsEvent(IList<T> warnings, IList<U> shoppingCartItems, IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes)
        {
            _warnings = warnings;
            _shoppingCartItems = shoppingCartItems;
            _checkoutAttributes = checkoutAttributes;
            _validateCheckoutAttributes = validateCheckoutAttributes;
        }
        public IList<T> Warnings => _warnings;
        public IList<U> ShoppingCartItems => _shoppingCartItems;

        public IList<CustomAttribute> CheckoutAttributes => _checkoutAttributes;

        public bool ValidateCheckoutAttributes => _validateCheckoutAttributes;
    }
}
