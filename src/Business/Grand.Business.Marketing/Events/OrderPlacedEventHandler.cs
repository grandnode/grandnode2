using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.Marketing.Events
{
    public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
    {
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly IRepository<CustomerReminderHistory> _customerReminderHistory;

        public OrderPlacedEventHandler(ICustomerActionEventService customerActionEventService,
            IRepository<CustomerReminderHistory> customerReminderHistory)
        {
            _customerActionEventService = customerActionEventService;
            _customerReminderHistory = customerReminderHistory;
        }

        public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            //cutomer action - add order
            await _customerActionEventService.AddOrder(notification.Order, CustomerActionTypeEnum.AddOrder);

            //customer reminder
            var update = UpdateBuilder<CustomerReminderHistory>.Create()
                .Set(x => x.EndDate, DateTime.UtcNow)
                .Set(x => x.Status, CustomerReminderHistoryStatusEnum.CompletedOrdered)
                .Set(x => x.OrderId, notification.Order.Id);

            _ = _customerReminderHistory.UpdateManyAsync(x => x.CustomerId == notification.Order.CustomerId && x.Status == CustomerReminderHistoryStatusEnum.Started, update);

        }
    }
}
