namespace Grand.Mediator;

/// <summary>
///     A request handled by exactly one handler and returning no response.
/// </summary>
public interface IRequest : IBaseRequest;

/// <summary>
///     A request handled by exactly one handler and returning <typeparamref name="TResponse" />.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <remarks>
///     Deliberately not derived from <see cref="IRequest" /> - the two are disjoint so that the
///     Send overloads resolve unambiguously.
/// </remarks>
public interface IRequest<out TResponse> : IBaseRequest;
