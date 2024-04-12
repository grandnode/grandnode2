using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Customers.Events.Handlers;

public class CustomerLoginFailedEventHandler : INotificationHandler<CustomerLoginFailedEvent>
{
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;

    public CustomerLoginFailedEventHandler(ICustomerService customerService, CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _customerSettings = customerSettings;
    }

    public async Task Handle(CustomerLoginFailedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Customer != null)
        {
            //wrong password
            notification.Customer.FailedLoginAttempts++;
            if (_customerSettings.FailedPasswordAllowedAttempts > 0 &&
                notification.Customer.FailedLoginAttempts >= _customerSettings.FailedPasswordAllowedAttempts)
            {
                //lock out
                notification.Customer.CannotLoginUntilDateUtc =
                    DateTime.UtcNow.AddMinutes(_customerSettings.FailedPasswordLockoutMinutes);
                //reset the counter
                notification.Customer.FailedLoginAttempts = 0;
            }

            await _customerService.UpdateCustomerLastLoginDate(notification.Customer);
        }
    }
}