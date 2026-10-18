using Aspire.ServiceDefaults.Telemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var settings = TelemetrySettings.Resolve(builder.Configuration, builder.Environment);
        if (!settings.Enabled)
            return builder;

        if (settings.UsesLegacyAzureMonitorKey)
            builder.Services.AddHostedService<LegacyTelemetryConfigWarning>();

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(settings.ServiceName, serviceNamespace: "grandnode", serviceVersion: settings.ServiceVersion,
                    autoGenerateServiceInstanceId: false, serviceInstanceId: Environment.MachineName)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)])
                //added last so OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES win over the values above
                .AddEnvironmentVariableDetector())
            .WithLogging(logging =>
            {
                if (settings.AzureMonitorConnectionString is not null)
                    logging.AddAzureMonitorLogExporter(o => o.ConnectionString = settings.AzureMonitorConnectionString);
                if (settings.Console)
                    logging.AddConsoleExporter();
            }, options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (settings.AzureMonitorConnectionString is not null)
                    metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = settings.AzureMonitorConnectionString);
                if (settings.Console)
                    metrics.AddConsoleExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context => TelemetryPathFilter.ShouldTrace(context.Request.Path, settings.ExcludePaths);
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                if (settings.AzureMonitorConnectionString is not null)
                    tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = settings.AzureMonitorConnectionString);
                if (settings.Console)
                    tracing.AddConsoleExporter();

                //after the exporters: the Azure Monitor trace exporter sets its own sampler and the last one wins
                var sampler = settings.CreateSampler();
                if (sampler is not null)
                    tracing.SetSampler(sampler);
            });

        if (settings.OtlpEndpoint is not null)
        {
            if (!settings.OtlpFromEnvironment)
                builder.Configuration.AddOtlpSettings(settings);

            //the exporter reads the OTEL_EXPORTER_OTLP_* keys itself
            telemetry.UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    ///     Exposes Telemetry:Otlp under the standard OTEL_EXPORTER_OTLP_* keys, skipping any key an environment
    ///     variable already sets, so the variables keep winning.
    /// </summary>
    private static void AddOtlpSettings(this IConfigurationManager configuration, TelemetrySettings settings)
    {
        var values = new Dictionary<string, string?> {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = settings.OtlpEndpoint,
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = settings.OtlpProtocol == OtlpExportProtocol.HttpProtobuf ? "http/protobuf" : "grpc",
            ["OTEL_EXPORTER_OTLP_HEADERS"] = settings.OtlpHeaders
        };

        configuration.AddInMemoryCollection(values
            .Where(pair => pair.Value is not null && string.IsNullOrWhiteSpace(configuration[pair.Key])));
    }
}
