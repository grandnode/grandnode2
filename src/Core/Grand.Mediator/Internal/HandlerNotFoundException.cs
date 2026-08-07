namespace Grand.Mediator.Internal;

/// <summary>
///     Thrown when a request reaches the mediator with no handler registered for it.
/// </summary>
internal static class HandlerNotFoundException
{
    public static InvalidOperationException For(Type requestType)
    {
        return new InvalidOperationException(
            $"Handler was not found for request of type {requestType}. Register your handlers with the container. " +
            "See the samples in Grand.Infrastructure.StartupBase.");
    }
}
