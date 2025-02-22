using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Business.Checkout.Events.ShoppingCart;

public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IContextAccessor _contextAccessor;

    public CustomerLoggedInEventHandler(IShoppingCartService shoppingCartService, IContextAccessor contextAccessor)
    {
        _shoppingCartService = shoppingCartService;
        _contextAccessor = contextAccessor;
    }

    public async Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
    {
        //migrate shopping cart
        await _shoppingCartService.MigrateShoppingCart(_contextAccessor.WorkContext.CurrentCustomer, notification.Customer, true);
    }
}