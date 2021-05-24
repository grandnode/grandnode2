using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Extensions
{
    public static class ShoppingCartEventPublisherExtensions
    {
        public static async Task ShoppingCartWarningsAdd<T, U>(this IMediator eventPublisher, IList<T> warnings, IList<U> shoppingCartItems, IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes) where U : ShoppingCartItem
        {
            await eventPublisher.Publish(new ShoppingCartWarningsEvent<T, U>(warnings, shoppingCartItems, checkoutAttributes, validateCheckoutAttributes));
        }

        public static async Task ShoppingCartItemWarningsAdded<C, S, P>(this IMediator eventPublisher, IList<string> warnings, C customer, S shoppingcartItem, P product) where C : Customer where S : ShoppingCartItem where P : Product
        {
            await eventPublisher.Publish(new ShoppingCartItemWarningsEvent<C, S, P>(warnings, customer, shoppingcartItem, product));
        }

    }
}