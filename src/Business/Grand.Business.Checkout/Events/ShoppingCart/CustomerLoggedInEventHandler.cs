using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Customers.Events;
using Grand.Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Events.ShoppingCart
{
    public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;

        public CustomerLoggedInEventHandler(IShoppingCartService shoppingCartService, IWorkContext workContext)
        {
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
        }

        public async Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
        {
            //migrate shopping cart
            await _shoppingCartService.MigrateShoppingCart(_workContext.CurrentCustomer, notification.Customer, true);

        }
    }
}
