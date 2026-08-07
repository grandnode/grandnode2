namespace Grand.Mediator;

/// <summary>
///     Marker shared by every request, regardless of whether it returns a response.
///     Used to validate the weakly typed <see cref="IMediator.Send(object, CancellationToken)" /> overload.
/// </summary>
public interface IBaseRequest;
