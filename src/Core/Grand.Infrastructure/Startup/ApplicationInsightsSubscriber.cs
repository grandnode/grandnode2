using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Events;
using System.Collections.Concurrent;
using System.Reflection;

namespace Grand.Infrastructure.Startup;

public class ApplicationInsightsSubscriber : IEventSubscriber
{
    private readonly ConcurrentDictionary<int, Operation> _dictionary = new();
    private readonly IServiceCollection _services;
    private readonly ReflectionEventSubscriber _subscriber;

    private TelemetryClient _telemetryClient;

    public ApplicationInsightsSubscriber(IServiceCollection services)
    {
        _subscriber = new ReflectionEventSubscriber(this,
            bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);

        _services = services;
    }

    private TelemetryClient TelemetryClient =>
        _telemetryClient ??= _services.BuildServiceProvider().GetRequiredService<TelemetryClient>();

    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
    {
        return _subscriber.TryGetEventHandler(out handler);
    }

    private void Handle(CommandStartedEvent cmdStart)
    {
        _dictionary.TryAdd(cmdStart.RequestId, new Operation(cmdStart.CommandName, cmdStart.Command.ToString()));
    }

    private void Handle(CommandSucceededEvent cmdSuccess)
    {
        if (!_dictionary.TryGetValue(cmdSuccess.RequestId, out var operation)) return;
        var telemetry = new DependencyTelemetry {
            Data = operation.Command,
            Name = operation.Name,
            Type = "MongoDB",
            Timestamp = DateTime.UtcNow.Subtract(cmdSuccess.Duration),
            Duration = cmdSuccess.Duration,
            Success = true
        };
        TelemetryClient?.TrackDependency(telemetry);

        _dictionary.TryRemove(cmdSuccess.RequestId, out _);
    }

    private record Operation(string Name, string Command);
}