using Grand.Business.Core.Events.Checkout.ShoppingCart;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using MediatR;

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
