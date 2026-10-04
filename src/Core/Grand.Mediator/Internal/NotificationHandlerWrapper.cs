using Microsoft.Extensions.DependencyInjection;

namespace Grand.Mediator.Internal;

internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider provider, CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(object notification, IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        //sequential, fail fast - a throwing handler aborts the remaining ones and surfaces in the caller
        foreach (var handler in provider.GetServices<INotificationHandler<TNotification>>())
            await handler.Handle((TNotification)notification, cancellationToken);
    }
}
