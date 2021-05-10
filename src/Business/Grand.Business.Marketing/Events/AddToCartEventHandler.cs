using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Marketing.Interfaces.Customers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Events
{
    public class AddToCartEventHandler : INotificationHandler<AddToCartEvent>
    {
        private readonly ICustomerActionEventService _customerActionEventService;

        public AddToCartEventHandler(ICustomerActionEventService customerActionEventService)
        {
            _customerActionEventService = customerActionEventService;
        }

        public async Task Handle(AddToCartEvent notification, CancellationToken cancellationToken)
        {
            await _customerActionEventService.AddToCart(notification.ShoppingCartItem, notification.Product, notification.Customer);
        }
    }
}
