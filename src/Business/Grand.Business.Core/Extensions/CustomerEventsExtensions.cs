using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Utilities.Customers;
using MediatR;

namespace Grand.Business.Core.Extensions;

public static class CustomerEventsExtensions
{
    public static async Task CustomerRegistrationEvent<R>(this IMediator eventPublisher, R request)
        where R : RegistrationRequest
    {
        await eventPublisher.Publish(new CustomerRegistrationEvent<R>(request));
    }
}