namespace Grand.Mediator;

/// <summary>
///     Handles a published notification. Any number of handlers may subscribe to the same notification.
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    ///     Handles the notification
    /// </summary>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
