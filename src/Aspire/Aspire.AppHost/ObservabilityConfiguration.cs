using Microsoft.Extensions.Configuration;

namespace Aspire.AppHost;

public static class ObservabilityConfiguration
{
    public static void AddGrafanaObservability(this IDistributedApplicationBuilder builder)
    {
        //opt-in: two more containers to pull and run, which a plain run of the store does not need
        if (!builder.Configuration.GetValue<bool>("Observability:Grafana"))
            return;

        //Tempo, Loki, Prometheus and Grafana in one container, receiving OTLP itself
        var grafana = builder.AddContainer("grafana", "grafana/otel-lgtm", "0.40.0")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithVolume("grandnode-grafana", "/data")
            .WithHttpEndpoint(targetPort: 3000)
            .WithEndpoint(targetPort: 4317, name: "otlp", scheme: "http");

        //an app exports to one OTLP endpoint only, which Aspire points at its dashboard - the collector takes
        //that place and sends everything on to both the dashboard and Grafana
        builder.AddOpenTelemetryCollector("otel-collector", settings => settings.CollectorTag = "0.165.0")
            .WithConfig(Path.Combine(builder.AppHostDirectory, "otel-collector.yaml"))
            .WithEnvironment("GRAFANA_OTLP_ENDPOINT", grafana.GetEndpoint("otlp"))
            .WaitFor(grafana)
            .WithAppForwarding();
    }
}
