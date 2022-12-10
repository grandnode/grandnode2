using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart
{
    public class AddToCartEvent : INotification
    {
        private readonly Product _product;
        private readonly Customer _customer;
        private readonly ShoppingCartItem _shoppingCartItem;

        public AddToCartEvent(Customer customer, ShoppingCartItem shoppingCartItem, Product product)
        {
            _customer = customer;
            _shoppingCartItem = shoppingCartItem;
            _product = product;
        }

        public Customer Customer => _customer;
        public ShoppingCartItem ShoppingCartItem => _shoppingCartItem;
        public Product Product => _product;
    }
}
