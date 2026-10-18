using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;

namespace Aspire.ServiceDefaults.Telemetry;

/// <summary>
///     The telemetry configuration the pipeline is built from: the "Telemetry" section merged with the standard
///     environment variables, which win, and the legacy ApplicationInsights:ConnectionString key, which loses.
/// </summary>
public sealed record TelemetrySettings(
    bool Enabled,
    string ServiceName,
    string? ServiceVersion,
    string? OtlpEndpoint,
    OtlpExportProtocol OtlpProtocol,
    string? OtlpHeaders,
    bool OtlpFromEnvironment,
    string? AzureMonitorConnectionString,
    bool UsesLegacyAzureMonitorKey,
    bool Console,
    double SamplingRatio,
    bool SamplerFromEnvironment,
    IReadOnlyList<string> ExcludePaths)
{
    public const string LegacyAzureMonitorKey = "ApplicationInsights:ConnectionString";

    private static readonly string[] DefaultExcludePaths = ["/health", "/_content", "/bundles"];

    public bool HasAnySink => OtlpEndpoint is not null || AzureMonitorConnectionString is not null || Console;

    public static TelemetrySettings Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var config = new TelemetryConfig();
        configuration.GetSection(TelemetryConfig.SectionName).Bind(config);

        if (config.Tracing.SamplingRatio is < 0 or > 1)
            throw new InvalidOperationException(
                $"{TelemetryConfig.SectionName}:Tracing:SamplingRatio must be between 0 and 1, was {config.Tracing.SamplingRatio}.");

        var protocol = config.Otlp.Protocol?.Trim().ToLowerInvariant() switch {
            null or "" or "grpc" => OtlpExportProtocol.Grpc,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"{TelemetryConfig.SectionName}:Otlp:Protocol must be \"grpc\" or \"http/protobuf\", was \"{config.Otlp.Protocol}\".")
        };

        var envOtlpEndpoint = Value(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        var otlpEndpoint = envOtlpEndpoint ?? Value(config.Otlp.Endpoint);
        if (envOtlpEndpoint is null && otlpEndpoint is not null && !Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException(
                $"{TelemetryConfig.SectionName}:Otlp:Endpoint must be an absolute URI, was \"{otlpEndpoint}\".");

        var azureConnectionString = Value(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])
                                    ?? Value(config.AzureMonitor.ConnectionString);
        var legacyConnectionString = azureConnectionString is null ? Value(configuration[LegacyAzureMonitorKey]) : null;

        return new TelemetrySettings(
            config.Enabled,
            Value(configuration["OTEL_SERVICE_NAME"]) ?? Value(config.ServiceName) ?? DefaultServiceName(environment.ApplicationName),
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            otlpEndpoint,
            protocol,
            Value(config.Otlp.Headers),
            envOtlpEndpoint is not null,
            azureConnectionString ?? legacyConnectionString,
            legacyConnectionString is not null,
            config.Console,
            config.Tracing.SamplingRatio,
            Value(configuration["OTEL_TRACES_SAMPLER"]) is not null,
            config.Tracing.ExcludePaths ?? DefaultExcludePaths);
    }

    public static string DefaultServiceName(string applicationName)
    {
        return applicationName switch {
            "Grand.Web" => "grandnode-web",
            "Grand.Web.Admin" => "grandnode-admin",
            "Grand.Web.Store" => "grandnode-store",
            "Grand.Web.Vendor" => "grandnode-vendor",
            _ => applicationName.ToLowerInvariant()
        };
    }

    private static string? Value(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
