using Grand.Business.Core.Utilities.Customers;
using MediatR;

namespace Grand.Business.Core.Events.Customers;

public class CustomerRegistrationEvent<R> : INotification where R : RegistrationRequest
{
    public CustomerRegistrationEvent(R request)
    {
        Request = request;
    }

    public R Request { get; }
}