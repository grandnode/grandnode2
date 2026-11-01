using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Messages.Events;

public class CustomerDeletedEventHandler : INotificationHandler<EntityDeleted<Customer>>
{
    private readonly IOrderService _orderService;
    private readonly IQueuedEmailService _queuedEmailService;

    public CustomerDeletedEventHandler(IOrderService orderService, IQueuedEmailService queuedEmailService)
    {
        _orderService = orderService;
        _queuedEmailService = queuedEmailService;
    }

    public async Task Handle(EntityDeleted<Customer> notification, CancellationToken cancellationToken)
    {
        //an invoice goes to the billing address of the order, which may not be the e-mail of the account
        var orders = await _orderService.SearchOrders(customerId: notification.Entity.Id);

        await _queuedEmailService.DeleteOrderInvoiceEmails(orders.Select(x => x.Id));
    }
}
