using System.Collections.Concurrent;
using Grand.Mediator.Internal;

namespace Grand.Mediator;

/// <summary>
///     Default <see cref="IMediator" /> implementation.
/// </summary>
/// <remarks>
///     Handlers are always resolved through <see cref="IServiceProvider" /> rather than an internal registry,
///     so hand-written registrations (see Grand.Module.Api, which registers closed generic handlers as scoped)
///     are honoured alongside the ones found by assembly scanning.
/// </remarks>
public sealed class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestHandlerBase>
        RequestHandlers = new();

    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> VoidRequestHandlers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationHandlers = new();

    private readonly IServiceProvider _provider;

    public Mediator(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestHandlers.GetOrAdd(
            (request.GetType(), typeof(TResponse)),
            static key => CreateWrapper(typeof(RequestHandlerWrapperImpl<,>), key.RequestType, key.ResponseType));

        return wrapper.HandleTyped(request, _provider, cancellationToken);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (VoidRequestHandlerWrapper)VoidRequestHandlers.GetOrAdd(
            request.GetType(),
            static requestType => CreateWrapper(typeof(VoidRequestHandlerWrapperImpl<>), requestType));

        return wrapper.HandleVoid(request, _provider, cancellationToken);
    }

    public Task<object> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (request is not IBaseRequest)
            throw new ArgumentException($"{requestType} does not implement {nameof(IRequest)}", nameof(request));

        var responseType = ResponseTypeOf(requestType);

        var wrapper = responseType is null
            ? VoidRequestHandlers.GetOrAdd(requestType,
                static type => CreateWrapper(typeof(VoidRequestHandlerWrapperImpl<>), type))
            : RequestHandlers.GetOrAdd((requestType, responseType),
                static key => CreateWrapper(typeof(RequestHandlerWrapperImpl<,>), key.RequestType, key.ResponseType));

        return wrapper.Handle(request, _provider, cancellationToken);
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        return PublishNotification(notification, cancellationToken);
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification)
            throw new ArgumentException(
                $"{notification.GetType()} does not implement {nameof(INotification)}", nameof(notification));

        return PublishNotification(notification, cancellationToken);
    }

    /// <summary>
    ///     Dispatches on the runtime type of the notification, so generic events such as EntityInserted&lt;Product&gt;
    ///     reach handlers registered for the closed type.
    /// </summary>
    private Task PublishNotification(object notification, CancellationToken cancellationToken)
    {
        var wrapper = NotificationHandlers.GetOrAdd(
            notification.GetType(),
            static notificationType => (NotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType))!);

        return wrapper.Handle(notification, _provider, cancellationToken);
    }

    /// <summary>
    ///     Returns the response type of a request, or null when the request returns none.
    /// </summary>
    private static Type ResponseTypeOf(Type requestType)
    {
        foreach (var contract in requestType.GetInterfaces())
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
                return contract.GenericTypeArguments[0];

        return null;
    }

    private static RequestHandlerBase CreateWrapper(Type openWrapperType, params Type[] typeArguments)
    {
        return (RequestHandlerBase)Activator.CreateInstance(openWrapperType.MakeGenericType(typeArguments))!;
    }
}
