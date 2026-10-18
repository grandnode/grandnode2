using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Trace;

namespace Aspire.ServiceDefaults.Tests;

[TestClass]
public class TelemetryPipelineTests
{
    //an unreachable endpoint - nothing is exported within a test
    private const string AzureConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:9/";

    private static (IHost Host, string SourceName) BuildHost(Dictionary<string, string?> values)
    {
        //the pipeline subscribes to the ActivitySource named after the application
        var sourceName = $"Telemetry.Tests.{Guid.NewGuid():N}";
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings {
            ApplicationName = sourceName,
            EnvironmentName = "Production"
        });
        builder.Configuration.AddInMemoryCollection(values);
        builder.ConfigureOpenTelemetry();

        var host = builder.Build();
        //resolving the provider builds it and attaches its ActivityListener
        host.Services.GetService<TracerProvider>();
        return (host, sourceName);
    }

    private static bool IsRootActivityRecorded(string sourceName)
    {
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("test");
        return activity?.Recorded == true;
    }

    [TestMethod]
    public void Disabled_RegistersNoProvider()
    {
        var (host, _) = BuildHost(new() {
            ["Telemetry:Enabled"] = "false",
            ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString
        });
        using (host)
        {
            Assert.IsNull(host.Services.GetService<TracerProvider>());
        }
    }

    [TestMethod]
    public void NoSink_FullRatio_RecordsActivities()
    {
        var (host, sourceName) = BuildHost([]);
        using (host)
        {
            Assert.IsTrue(IsRootActivityRecorded(sourceName));
        }
    }

    [TestMethod]
    public void ZeroRatio_DropsRootActivities()
    {
        var (host, sourceName) = BuildHost(new() { ["Telemetry:Tracing:SamplingRatio"] = "0" });
        using (host)
        {
            Assert.IsFalse(IsRootActivityRecorded(sourceName));
        }
    }

    [TestMethod]
    public void AzureMonitor_OurSamplingRatioWinsOverTheExporterSampler()
    {
        //the Azure Monitor trace exporter installs a sampler limited to 5 traces per second;
        //with our ratio of 1 every one of a quick burst of traces must still be recorded
        var (host, sourceName) = BuildHost(new() {
            ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString,
            ["Telemetry:Tracing:SamplingRatio"] = "1"
        });
        using (host)
        {
            var recorded = Enumerable.Range(0, 50).Count(_ => IsRootActivityRecorded(sourceName));
            Assert.AreEqual(50, recorded);
        }
    }

    [TestMethod]
    public void OtelTracesSampler_WinsOverSamplingRatio()
    {
        var (host, sourceName) = BuildHost(new() {
            ["OTEL_TRACES_SAMPLER"] = "always_on",
            ["Telemetry:Tracing:SamplingRatio"] = "0"
        });
        using (host)
        {
            Assert.IsTrue(IsRootActivityRecorded(sourceName));
        }
    }

    [TestMethod]
    public void AzureMonitor_OtelTracesSamplerWinsOverTheExporterSampler()
    {
        var (host, sourceName) = BuildHost(new() {
            ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString,
            ["OTEL_TRACES_SAMPLER"] = "always_off"
        });
        using (host)
        {
            var recorded = Enumerable.Range(0, 50).Count(_ => IsRootActivityRecorded(sourceName));
            Assert.AreEqual(0, recorded);
        }
    }

    [TestMethod]
    public void OtlpEndpointInFile_IsExposedUnderTheStandardKeys()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Telemetry:Otlp:Endpoint"] = "http://collector:4318",
            ["Telemetry:Otlp:Protocol"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_HEADERS"] = "x-api-key=from-env",
            ["Telemetry:Otlp:Headers"] = "x-api-key=from-file"
        });

        builder.ConfigureOpenTelemetry();

        Assert.AreEqual("http://collector:4318", builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        Assert.AreEqual("http/protobuf", builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]);
        Assert.AreEqual("x-api-key=from-env", builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]);
    }
}
