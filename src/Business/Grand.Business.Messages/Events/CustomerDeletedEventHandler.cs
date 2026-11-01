using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Messages.Events;

public class CustomerDeletedEventHandler : INotificationHandler<EntityDeleted<Customer>>
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IQueuedEmailService _queuedEmailService;

    public CustomerDeletedEventHandler(IRepository<Order> orderRepository, IQueuedEmailService queuedEmailService)
    {
        _orderRepository = orderRepository;
        _queuedEmailService = queuedEmailService;
    }

    public async Task Handle(EntityDeleted<Customer> notification, CancellationToken cancellationToken)
    {
        //an invoice goes to the billing address of the order, which may not be the e-mail of the account;
        //a deleted order counts too, as its invoice email stays in the queue
        var customerId = notification.Entity.Id;
        var orderIds = await _orderRepository.ToListAsync(
            _orderRepository.Table.Where(x => x.CustomerId == customerId).Select(x => x.Id), cancellationToken);

        await _queuedEmailService.DeleteOrderInvoiceEmails(orderIds);
    }
}
