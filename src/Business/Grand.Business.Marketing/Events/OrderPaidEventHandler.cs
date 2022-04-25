using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Marketing.Events
{
    public class OrderPaidEventHandler : INotificationHandler<OrderPaidEvent>
    {
        private readonly ICustomerActionEventService _customerActionEventService;

        public OrderPaidEventHandler(ICustomerActionEventService customerActionEventService)
        {
            _customerActionEventService = customerActionEventService;
        }

        public async Task Handle(OrderPaidEvent notification, CancellationToken cancellationToken)
        {
            //customer action event service - paid order
            await _customerActionEventService.AddOrder(notification.Order, CustomerActionTypeEnum.PaidOrder);
        }
    }
}
