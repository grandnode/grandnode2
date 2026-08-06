using Microsoft.Extensions.DependencyInjection;

namespace Grand.Mediator.Internal;

/// <summary>
///     Non-generic entry point used by the weakly typed Send overload.
/// </summary>
internal abstract class RequestHandlerBase
{
    public abstract Task<object> Handle(object request, IServiceProvider provider,
        CancellationToken cancellationToken);
}

/// <summary>
///     Bridges the statically known response type to a handler resolved at runtime.
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> HandleTyped(object request, IServiceProvider provider,
        CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> HandleTyped(object request, IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        return Resolve(provider).Handle((TRequest)request, cancellationToken);
    }

    public override async Task<object> Handle(object request, IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        return await Resolve(provider).Handle((TRequest)request, cancellationToken);
    }

    private static IRequestHandler<TRequest, TResponse> Resolve(IServiceProvider provider)
    {
        return provider.GetService<IRequestHandler<TRequest, TResponse>>() ??
               throw HandlerNotFoundException.For(typeof(TRequest));
    }
}

/// <summary>
///     Wrapper for requests that return no response.
/// </summary>
internal abstract class VoidRequestHandlerWrapper : RequestHandlerBase
{
    public abstract Task HandleVoid(object request, IServiceProvider provider, CancellationToken cancellationToken);
}

internal sealed class VoidRequestHandlerWrapperImpl<TRequest> : VoidRequestHandlerWrapper
    where TRequest : IRequest
{
    public override Task HandleVoid(object request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        return Resolve(provider).Handle((TRequest)request, cancellationToken);
    }

    public override async Task<object> Handle(object request, IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        await Resolve(provider).Handle((TRequest)request, cancellationToken);
        return null;
    }

    private static IRequestHandler<TRequest> Resolve(IServiceProvider provider)
    {
        return provider.GetService<IRequestHandler<TRequest>>() ??
               throw HandlerNotFoundException.For(typeof(TRequest));
    }
}
