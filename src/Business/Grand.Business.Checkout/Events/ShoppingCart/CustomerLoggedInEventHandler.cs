using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Business.Checkout.Events.ShoppingCart;

public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public CustomerLoggedInEventHandler(IShoppingCartService shoppingCartService, IWorkContextAccessor workContextAccessor)
    {
        _shoppingCartService = shoppingCartService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
    {
        //migrate shopping cart
        await _shoppingCartService.MigrateShoppingCart(_workContextAccessor.WorkContext.CurrentCustomer, notification.Customer, true);
    }
}