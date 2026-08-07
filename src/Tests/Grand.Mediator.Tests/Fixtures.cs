namespace Grand.Mediator.Tests;

/// <summary>
///     Records what ran, so tests assert on ordering without static state
/// </summary>
public class ExecutionLog
{
    public List<string> Entries { get; } = [];
}

#region Requests

public class PingQuery : IRequest<string>
{
    public string Message { get; set; }
}

public class PingQueryHandler : IRequestHandler<PingQuery, string>
{
    public Task<string> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"pong:{request.Message}");
    }
}

public class UnhandledQuery : IRequest<string>;

public class VoidCommand : IRequest;

public class VoidCommandHandler : IRequestHandler<VoidCommand>
{
    private readonly ExecutionLog _log;

    public VoidCommandHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(VoidCommand request, CancellationToken cancellationToken)
    {
        _log.Entries.Add(nameof(VoidCommandHandler));
        return Task.CompletedTask;
    }
}

public class UnhandledCommand : IRequest;

/// <summary>
///     Mirrors Grand.Module.Api, which registers this shape closed over each entity pair, by hand and as scoped
/// </summary>
public class GenericQuery<T> : IRequest<IList<T>>;

public class GenericQueryHandler<T> : IRequestHandler<GenericQuery<T>, IList<T>>
{
    public Task<IList<T>> Handle(GenericQuery<T> request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<T>>([default]);
    }
}

#endregion

#region Notifications

public class SampleEvent : INotification;

public class FirstSampleEventHandler : INotificationHandler<SampleEvent>
{
    private readonly ExecutionLog _log;

    public FirstSampleEventHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(SampleEvent notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add(nameof(FirstSampleEventHandler));
        return Task.CompletedTask;
    }
}

public class SecondSampleEventHandler : INotificationHandler<SampleEvent>
{
    private readonly ExecutionLog _log;

    public SecondSampleEventHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(SampleEvent notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add(nameof(SecondSampleEventHandler));
        return Task.CompletedTask;
    }
}

public class UnobservedEvent : INotification;

/// <summary>
///     Mirrors the shape of EntityInserted&lt;T&gt; - published as a closed generic, handled per entity
/// </summary>
public class EntityChanged<T> : INotification
{
    public EntityChanged(T entity)
    {
        Entity = entity;
    }

    public T Entity { get; }
}

public class EntityChangedStringHandler : INotificationHandler<EntityChanged<string>>
{
    private readonly ExecutionLog _log;

    public EntityChangedStringHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(EntityChanged<string> notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"{nameof(EntityChangedStringHandler)}:{notification.Entity}");
        return Task.CompletedTask;
    }
}

public class EntityChangedIntHandler : INotificationHandler<EntityChanged<int>>
{
    private readonly ExecutionLog _log;

    public EntityChangedIntHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(EntityChanged<int> notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"{nameof(EntityChangedIntHandler)}:{notification.Entity}");
        return Task.CompletedTask;
    }
}

public class FailingEvent : INotification;

public class ThrowingEventHandler : INotificationHandler<FailingEvent>
{
    private readonly ExecutionLog _log;

    public ThrowingEventHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(FailingEvent notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add(nameof(ThrowingEventHandler));
        throw new InvalidTimeZoneException("handler failed");
    }
}

public class NeverReachedEventHandler : INotificationHandler<FailingEvent>
{
    private readonly ExecutionLog _log;

    public NeverReachedEventHandler(ExecutionLog log)
    {
        _log = log;
    }

    public Task Handle(FailingEvent notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add(nameof(NeverReachedEventHandler));
        return Task.CompletedTask;
    }
}

#endregion
