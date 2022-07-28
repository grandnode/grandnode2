using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Events;
using System.Collections.Concurrent;
using System.Reflection;

namespace Grand.Infrastructure.Startup
{
    public class ApplicationInsightsSubscriber : IEventSubscriber
    {
        private readonly ReflectionEventSubscriber _subscriber;

        private TelemetryClient _telementryClient;
        protected TelemetryClient TelementryClient {
            get {
                if (_telementryClient == null)
                {
                    _telementryClient = _services.BuildServiceProvider().GetRequiredService<TelemetryClient>();
                }
                return _telementryClient;
            }
        }
        private readonly IServiceCollection _services;

        private readonly ConcurrentDictionary<int, Operation> dictionary = new ();

        record Operation(string Name, string Command);

        public ApplicationInsightsSubscriber(IServiceCollection services)
        {
            _subscriber = new ReflectionEventSubscriber(this,
                bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);

            _services = services;
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _subscriber.TryGetEventHandler(out handler);
        }

        private void Handle(CommandStartedEvent cmdStart)
        {
            dictionary.TryAdd(cmdStart.RequestId, new Operation(cmdStart.CommandName, cmdStart.Command.ToString()));
        }

        private void Handle(CommandSucceededEvent cmdSuccess)
        {
            if (dictionary.TryGetValue(cmdSuccess.RequestId, out var operation))
            {
                var telemetry = new DependencyTelemetry {
                    Data = operation.Command,                    
                    Name = operation.Name,
                    Type = "MongoDB",
                    Timestamp = DateTime.UtcNow.Subtract(cmdSuccess.Duration),
                    Duration = cmdSuccess.Duration,
                    Success = true,
                };
                TelementryClient?.TrackDependency(telemetry);

                dictionary.TryRemove(cmdSuccess.RequestId, out var _);
            }
        }
    }
}
