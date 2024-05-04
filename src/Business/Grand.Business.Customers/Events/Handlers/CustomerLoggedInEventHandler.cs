using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Business.Customers.Events.Handlers;

public class CustomerLoggedInEventHandler : INotificationHandler<CustomerLoggedInEvent>
{
    private readonly ICustomerService _customerService;

    public CustomerLoggedInEventHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Customer != null)
        {
            //save last login date
            notification.Customer.FailedLoginAttempts = 0;
            notification.Customer.CannotLoginUntilDateUtc = null;
            notification.Customer.LastLoginDateUtc = DateTime.UtcNow;
            await _customerService.UpdateCustomerLastLoginDate(notification.Customer);
        }
    }
}