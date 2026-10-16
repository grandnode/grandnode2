using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.ShoppingCart;

public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
{
    private readonly IShoppingCartService _shoppingCartService;

    public CustomerLoggedInEventHandler(IShoppingCartService shoppingCartService)
    {
        _shoppingCartService = shoppingCartService;
    }

    public async Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
    {
        //migrate shopping cart
        await _shoppingCartService.MigrateShoppingCart(notification.PreviousCustomer, notification.Customer,
            notification.StoreId, true);
    }
}