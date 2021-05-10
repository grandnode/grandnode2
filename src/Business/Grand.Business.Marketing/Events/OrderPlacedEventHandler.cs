using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Marketing.Commands.Models;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Domain.Customers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Events
{
    public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
    {
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly IMediator _mediator;

        public OrderPlacedEventHandler(ICustomerActionEventService customerActionEventService, IMediator mediator)
        {
            _customerActionEventService = customerActionEventService;
            _mediator = mediator;
        }

        public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            //cutomer action - add order
            await _customerActionEventService.AddOrder(notification.Order, CustomerActionTypeEnum.AddOrder);

            //customer reminder
            await _mediator.Send(new UpdateCustomerReminderHistoryCommand() { CustomerId = notification.Order.CustomerId, OrderId = notification.Order.Id });
        }
    }
}
