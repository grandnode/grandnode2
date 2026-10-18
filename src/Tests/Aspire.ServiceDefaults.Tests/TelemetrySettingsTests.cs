using Aspire.ServiceDefaults.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Exporter;

namespace Aspire.ServiceDefaults.Tests;

[TestClass]
public class TelemetrySettingsTests
{
    private static TelemetrySettings Resolve(Dictionary<string, string?> values, string applicationName = "Grand.Web")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return TelemetrySettings.Resolve(configuration, new TestHostEnvironment(applicationName));
    }

    [TestMethod]
    [DataRow("Grand.Web", "grandnode-web")]
    [DataRow("Grand.Web.Admin", "grandnode-admin")]
    [DataRow("Grand.Web.Store", "grandnode-store")]
    [DataRow("Grand.Web.Vendor", "grandnode-vendor")]
    [DataRow("My.Custom.Host", "my.custom.host")]
    public void Resolve_NoServiceName_UsesPerHostDefault(string applicationName, string expected)
    {
        var settings = Resolve([], applicationName);

        Assert.AreEqual(expected, settings.ServiceName);
    }

    [TestMethod]
    public void Resolve_ServiceNameInFile_WinsOverDefault()
    {
        var settings = Resolve(new() { ["Telemetry:ServiceName"] = "shop-eu" });

        Assert.AreEqual("shop-eu", settings.ServiceName);
    }

    [TestMethod]
    public void Resolve_OtelServiceName_WinsOverFile()
    {
        var settings = Resolve(new() {
            ["Telemetry:ServiceName"] = "shop-eu",
            ["OTEL_SERVICE_NAME"] = "grand-web"
        });

        Assert.AreEqual("grand-web", settings.ServiceName);
    }

    [TestMethod]
    public void Resolve_Defaults_EnabledWithoutSinks()
    {
        var settings = Resolve([]);

        Assert.IsTrue(settings.Enabled);
        Assert.IsFalse(settings.HasAnySink);
        Assert.IsNull(settings.OtlpEndpoint);
        Assert.IsNull(settings.AzureMonitorConnectionString);
        Assert.IsFalse(settings.Console);
        Assert.AreEqual(1.0, settings.SamplingRatio);
        Assert.AreEqual(OtlpExportProtocol.Grpc, settings.OtlpProtocol);
        CollectionAssert.AreEqual(new[] { "/health", "/_content", "/bundles", "/assets", "/theme", "/favicon.ico" }, settings.ExcludePaths.Select(p => p.Value).ToArray());
        Assert.IsFalse(settings.AzureSdkTracing);
    }

    [TestMethod]
    public void Resolve_Instrumentation_IsBoundFromFile()
    {
        var settings = Resolve(new() { ["Telemetry:Instrumentation:AzureSdk"] = "true" });

        Assert.IsTrue(settings.AzureSdkTracing);
    }

    [TestMethod]
    public void Resolve_NoExcludeExtensions_UsesStaticAssetDefaults()
    {
        var settings = Resolve([]);

        CollectionAssert.AreEqual(
            new[] {
                ".css", ".js", ".map", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp", ".avif", ".ico",
                ".woff", ".woff2", ".ttf", ".otf", ".eot"
            },
            settings.ExcludeExtensions.ToArray());
    }

    [TestMethod]
    public void Resolve_ExcludeExtensionsInFile_ReplaceDefaultsAndGetALeadingDot()
    {
        var settings = Resolve(new() {
            ["Telemetry:Tracing:ExcludeExtensions:0"] = "avif",
            ["Telemetry:Tracing:ExcludeExtensions:1"] = ".PDF"
        });

        CollectionAssert.AreEqual(new[] { ".avif", ".pdf" }, settings.ExcludeExtensions.ToArray());
    }

    [TestMethod]
    public void Resolve_ExcludePathsInFile_ReplaceDefaults()
    {
        var settings = Resolve(new() { ["Telemetry:Tracing:ExcludePaths:0"] = "/robots.txt" });

        CollectionAssert.AreEqual(new[] { "/robots.txt" }, settings.ExcludePaths.Select(p => p.Value).ToArray());
    }

    [TestMethod]
    public void Resolve_ExcludePathsInFile_DropEmptyEntriesAndGetALeadingSlash()
    {
        var settings = Resolve(new() {
            ["Telemetry:Tracing:ExcludePaths:0"] = "",
            ["Telemetry:Tracing:ExcludePaths:1"] = " health ",
            ["Telemetry:Tracing:ExcludePaths:2"] = "/",
            ["Telemetry:Tracing:ExcludePaths:3"] = "/api/status/"
        });

        CollectionAssert.AreEqual(new[] { "/health", "/api/status" }, settings.ExcludePaths.Select(p => p.Value).ToArray());
    }

    [TestMethod]
    public void Resolve_OtlpEndpointInFile_IsUsed()
    {
        var settings = Resolve(new() {
            ["Telemetry:Otlp:Endpoint"] = "http://collector:4318",
            ["Telemetry:Otlp:Protocol"] = "http/protobuf",
            ["Telemetry:Otlp:Headers"] = "x-api-key=abc"
        });

        Assert.IsTrue(settings.HasAnySink);
        Assert.AreEqual("http://collector:4318", settings.OtlpEndpoint);
        Assert.AreEqual(OtlpExportProtocol.HttpProtobuf, settings.OtlpProtocol);
        Assert.AreEqual("x-api-key=abc", settings.OtlpHeaders);
    }

    [TestMethod]
    public void Resolve_OtlpEndpointInEnvironment_WinsOverFile()
    {
        var settings = Resolve(new() {
            ["Telemetry:Otlp:Endpoint"] = "http://collector:4318",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317"
        });

        Assert.AreEqual("http://localhost:4317", settings.OtlpEndpoint);
    }

    [TestMethod]
    [DataRow("collector:4317")]
    [DataRow("ftp://collector:4317")]
    [DataRow("/collector")]
    public void Resolve_OtlpEndpointInFileWithoutHttpScheme_Throws(string endpoint)
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Resolve(new() { ["Telemetry:Otlp:Endpoint"] = endpoint }));

        StringAssert.Contains(ex.Message, "Telemetry:Otlp:Endpoint");
    }

    [TestMethod]
    public void Resolve_AzureMonitorEnvironmentVariable_WinsOverFile()
    {
        var settings = Resolve(new() {
            ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=env",
            ["Telemetry:AzureMonitor:ConnectionString"] = "InstrumentationKey=file"
        });

        Assert.AreEqual("InstrumentationKey=env", settings.AzureMonitorConnectionString);
    }

    [TestMethod]
    public void Resolve_OldApplicationInsightsKey_IsStillRead()
    {
        var settings = Resolve(new() { ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=old" });

        Assert.AreEqual("InstrumentationKey=old", settings.AzureMonitorConnectionString);
        Assert.IsTrue(settings.HasAnySink);
    }

    [TestMethod]
    public void Resolve_NewAzureMonitorKey_WinsOverOldApplicationInsightsKey()
    {
        var settings = Resolve(new() {
            ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=old",
            ["Telemetry:AzureMonitor:ConnectionString"] = "InstrumentationKey=file"
        });

        Assert.AreEqual("InstrumentationKey=file", settings.AzureMonitorConnectionString);
    }

    [TestMethod]
    public void Resolve_OtlpEndpointInEnvironment_IgnoresHeadersFromFile()
    {
        var settings = Resolve(new() {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
            ["Telemetry:Otlp:Endpoint"] = "https://otlp.vendor.com",
            ["Telemetry:Otlp:Headers"] = "x-api-key=secret"
        });

        Assert.AreEqual("http://localhost:4317", settings.OtlpEndpoint);
        Assert.IsNull(settings.OtlpHeaders);
    }

    [TestMethod]
    public void Resolve_ConsoleOn_CountsAsSink()
    {
        var settings = Resolve(new() { ["Telemetry:Console"] = "true" });

        Assert.IsTrue(settings.HasAnySink);
    }

    [TestMethod]
    public void CreateSampler_NoOtelTracesSampler_UsesParentBasedRatio()
    {
        var settings = Resolve(new() { ["Telemetry:Tracing:SamplingRatio"] = "0.25" });

        Assert.AreEqual("ParentBased{TraceIdRatioBasedSampler{0.250000}}", settings.CreateSampler()?.Description);
    }

    [TestMethod]
    [DataRow("always_on", null, "AlwaysOnSampler")]
    [DataRow("always_off", null, "AlwaysOffSampler")]
    [DataRow("traceidratio", "0.5", "TraceIdRatioBasedSampler{0.500000}")]
    [DataRow("parentbased_always_on", null, "ParentBased{AlwaysOnSampler}")]
    [DataRow("parentbased_always_off", null, "ParentBased{AlwaysOffSampler}")]
    [DataRow("parentbased_traceidratio", "0.1", "ParentBased{TraceIdRatioBasedSampler{0.100000}}")]
    [DataRow("parentbased_traceidratio", "nonsense", "ParentBased{TraceIdRatioBasedSampler{1.000000}}")]
    [DataRow("PARENTBASED_TRACEIDRATIO", null, "ParentBased{TraceIdRatioBasedSampler{1.000000}}")]
    public void CreateSampler_OtelTracesSampler_WinsOverSamplingRatio(string sampler, string? arg, string expected)
    {
        var settings = Resolve(new() {
            ["Telemetry:Tracing:SamplingRatio"] = "0.25",
            ["OTEL_TRACES_SAMPLER"] = sampler,
            ["OTEL_TRACES_SAMPLER_ARG"] = arg
        });

        Assert.AreEqual(expected, settings.CreateSampler()?.Description);
    }

    [TestMethod]
    public void CreateSampler_VendorOtelTracesSampler_IsLeftToTheExporter()
    {
        var settings = Resolve(new() { ["OTEL_TRACES_SAMPLER"] = "microsoft.rate_limited" });

        Assert.IsNull(settings.CreateSampler());
    }

    [TestMethod]
    [DataRow("-0.1")]
    [DataRow("1.5")]
    public void Resolve_SamplingRatioOutOfRange_Throws(string ratio)
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Resolve(new() { ["Telemetry:Tracing:SamplingRatio"] = ratio }));

        StringAssert.Contains(ex.Message, "Telemetry:Tracing:SamplingRatio");
    }

    [TestMethod]
    public void Resolve_UnknownProtocol_Throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Resolve(new() { ["Telemetry:Otlp:Protocol"] = "http" }));

        StringAssert.Contains(ex.Message, "Telemetry:Otlp:Protocol");
    }

    [TestMethod]
    public void Resolve_Disabled_IsReported()
    {
        var settings = Resolve(new() { ["Telemetry:Enabled"] = "false" });

        Assert.IsFalse(settings.Enabled);
    }

    private sealed class TestHostEnvironment(string applicationName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = applicationName;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
