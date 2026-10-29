using Grand.Business.Core.Events.Customers;
using Grand.Mediator;

namespace Grand.Web.Common.Audit;

/// <summary>
///     Records staff sign-ins, failed sign-ins and sign-outs from the events the login flows already publish
///     (storefront, panels and external sign-in)
/// </summary>
public class AuditSignInEventHandler(IAuditEventRecorder recorder) :
    INotificationHandler<CustomerLoggedInEvent>,
    INotificationHandler<CustomerLoginFailedEvent>,
    INotificationHandler<CustomerLoggedOutEvent>
{
    public Task Handle(CustomerLoggedInEvent notification, CancellationToken cancellationToken)
    {
        return recorder.SignIn(notification.Customer);
    }

    public Task Handle(CustomerLoginFailedEvent notification, CancellationToken cancellationToken)
    {
        return recorder.SignInFailed(notification.Customer, notification.Reason);
    }

    public Task Handle(CustomerLoggedOutEvent notification, CancellationToken cancellationToken)
    {
        return recorder.SignOut(notification.Customer);
    }
}
