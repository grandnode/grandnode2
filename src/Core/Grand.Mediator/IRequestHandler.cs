namespace Grand.Mediator;

/// <summary>
///     Handles a request returning <typeparamref name="TResponse" />.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     Handles the request
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
///     Handles a request returning no response.
/// </summary>
public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    ///     Handles the request
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TRequest request, CancellationToken cancellationToken);
}
