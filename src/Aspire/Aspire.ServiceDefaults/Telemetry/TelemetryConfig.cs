namespace Aspire.ServiceDefaults.Telemetry;

/// <summary>
///     The "Telemetry" section of appsettings.json. Standard OpenTelemetry environment variables
///     (OTEL_SERVICE_NAME, OTEL_EXPORTER_OTLP_*, OTEL_TRACES_SAMPLER*) and
///     APPLICATIONINSIGHTS_CONNECTION_STRING win over the values here.
/// </summary>
public class TelemetryConfig
{
    public const string SectionName = "Telemetry";

    /// <summary>
    ///     Gets or sets a value indicating whether OpenTelemetry is wired at all. With no sink configured
    ///     telemetry is collected in-process only and never exported.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the service.name resource attribute. Empty uses the per-host default
    ///     (grandnode-web, grandnode-admin, grandnode-store, grandnode-vendor).
    /// </summary>
    public string? ServiceName { get; set; }

    public OtlpConfig Otlp { get; set; } = new();

    public AzureMonitorConfig AzureMonitor { get; set; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether traces, metrics and logs are written to the console.
    ///     Meant for development only.
    /// </summary>
    public bool Console { get; set; }

    public TracingConfig Tracing { get; set; } = new();

    public InstrumentationConfig Instrumentation { get; set; } = new();

    public class OtlpConfig
    {
        /// <summary>
        ///     Gets or sets the OTLP endpoint (an OpenTelemetry Collector or any OTLP backend). Empty disables OTLP export.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        ///     Gets or sets the OTLP protocol: "grpc" or "http/protobuf".
        /// </summary>
        public string Protocol { get; set; } = "grpc";

        /// <summary>
        ///     Gets or sets the headers sent with every export, as comma separated key=value pairs.
        /// </summary>
        public string? Headers { get; set; }
    }

    public class AzureMonitorConfig
    {
        /// <summary>
        ///     Gets or sets the Application Insights connection string. Empty disables Azure Monitor export.
        /// </summary>
        public string? ConnectionString { get; set; }
    }

    public class TracingConfig
    {
        /// <summary>
        ///     Gets or sets the share of new traces that are recorded, from 0 to 1. A request that arrives with
        ///     a sampled parent is always recorded.
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;

        /// <summary>
        ///     Gets or sets the request path prefixes that never produce a trace. Not set uses the health checks
        ///     and the static content of wwwroot: /health, /_content, /bundles, /assets, /theme, /favicon.ico.
        ///     Do not add /Plugins - next to plugin assets it holds plugin endpoints such as payment webhooks.
        /// </summary>
        public string[]? ExcludePaths { get; set; }

        /// <summary>
        ///     Gets or sets the file extensions of requests that never produce a trace, e.g. plugin assets under
        ///     /Plugins. Not set uses the static asset types (.css, .js, images, fonts). Leave out .txt, .xml and
        ///     .json - robots.txt, sitemap.xml and JSON endpoints are served by controllers.
        /// </summary>
        public string[]? ExcludeExtensions { get; set; }
    }

    public class InstrumentationConfig
    {
        public MongoDbConfig MongoDb { get; set; } = new();

        /// <summary>
        ///     Gets or sets a value indicating whether the Azure SDK clients (Blob Storage, Key Vault) produce their
        ///     own spans on top of the HTTP spans they already have. The Azure SDK still marks its tracing as
        ///     experimental, so it is off by default.
        /// </summary>
        public bool AzureSdk { get; set; }
    }

    public class MongoDbConfig
    {
        /// <summary>
        ///     Gets or sets a value indicating whether MongoDB operations produce spans. With no sink configured they
        ///     are never produced - two activities per database call would cost without being exported. The query text is left out
        ///     unless QueryTextMaxLength is set - it is applied where the client is built, see
        ///     Grand.Infrastructure.Configuration.MongoDbTracingConfig.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
