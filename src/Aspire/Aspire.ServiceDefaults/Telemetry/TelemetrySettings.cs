using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using System.Globalization;

namespace Aspire.ServiceDefaults.Telemetry;

/// <summary>
///     The telemetry configuration the pipeline is built from: the "Telemetry" section merged with the standard
///     environment variables, which win, and the legacy ApplicationInsights:ConnectionString key, which loses
///     (still read in 2.5, removed in 2.6).
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
    bool Console,
    double SamplingRatio,
    string? TracesSampler,
    string? TracesSamplerArg,
    IReadOnlyList<string> ExcludePaths)
{
    private const string LegacyAzureMonitorKey = "ApplicationInsights:ConnectionString";

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
                                    ?? Value(config.AzureMonitor.ConnectionString)
                                    ?? Value(configuration[LegacyAzureMonitorKey]);

        return new TelemetrySettings(
            config.Enabled,
            Value(configuration["OTEL_SERVICE_NAME"]) ?? Value(config.ServiceName) ?? DefaultServiceName(environment.ApplicationName),
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            otlpEndpoint,
            protocol,
            Value(config.Otlp.Headers),
            envOtlpEndpoint is not null,
            azureConnectionString,
            config.Console,
            config.Tracing.SamplingRatio,
            Value(configuration["OTEL_TRACES_SAMPLER"])?.ToLowerInvariant(),
            Value(configuration["OTEL_TRACES_SAMPLER_ARG"]),
            config.Tracing.ExcludePaths ?? DefaultExcludePaths);
    }

    /// <summary>
    ///     The sampler for the trace pipeline: the standard OTEL_TRACES_SAMPLER values win over SamplingRatio.
    ///     Returns null for any other value (e.g. the Azure Monitor microsoft.* samplers), leaving the choice to
    ///     the exporter. Set explicitly because the Azure Monitor trace exporter installs its own sampler, which
    ///     would otherwise hide OTEL_TRACES_SAMPLER from the SDK.
    /// </summary>
    public Sampler? CreateSampler()
    {
        var ratio = double.TryParse(TracesSamplerArg, NumberStyles.Float, CultureInfo.InvariantCulture, out var arg) &&
                    arg is >= 0 and <= 1
            ? arg
            : 1.0;

        return TracesSampler switch {
            null => new ParentBasedSampler(new TraceIdRatioBasedSampler(SamplingRatio)),
            "always_on" => new AlwaysOnSampler(),
            "always_off" => new AlwaysOffSampler(),
            "traceidratio" => new TraceIdRatioBasedSampler(ratio),
            "parentbased_always_on" => new ParentBasedSampler(new AlwaysOnSampler()),
            "parentbased_always_off" => new ParentBasedSampler(new AlwaysOffSampler()),
            "parentbased_traceidratio" => new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)),
            _ => null
        };
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
