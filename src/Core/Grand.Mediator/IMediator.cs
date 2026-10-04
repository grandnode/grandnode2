namespace Grand.Mediator;

/// <summary>
///     Sends requests to a single handler and publishes notifications to every subscribed handler.
/// </summary>
public interface IMediator
{
    /// <summary>
    ///     Sends a request to its single handler
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the handler</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a request that returns no response to its single handler
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    ///     Sends a request whose type is only known at runtime
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the handler, or null for a request returning no response</returns>
    Task<object> Send(object request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes a notification to all of its handlers, sequentially
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    ///     Publishes a notification whose type is only known at runtime
    /// </summary>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Publish(object notification, CancellationToken cancellationToken = default);
}
