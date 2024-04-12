using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.ShoppingCart;

public class AddToCartEvent : INotification
{
    public AddToCartEvent(Customer customer, ShoppingCartItem shoppingCartItem, Product product)
    {
        Customer = customer;
        ShoppingCartItem = shoppingCartItem;
        Product = product;
    }

    public Customer Customer { get; }

    public ShoppingCartItem ShoppingCartItem { get; }

    public Product Product { get; }
}