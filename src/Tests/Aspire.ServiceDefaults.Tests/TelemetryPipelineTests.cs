using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aspire.ServiceDefaults.Telemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
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
    public void MongoDb_WithASink_RecordsDriverActivities()
    {
        var (host, _) = BuildHost(new() { ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString });
        using (host)
        {
            Assert.IsTrue(IsRootActivityRecorded("MongoDB.Driver"));
        }
    }

    [TestMethod]
    public void MongoDb_NoSink_DoesNotListenToTheDriver()
    {
        var (host, _) = BuildHost([]);
        using (host)
        {
            Assert.IsFalse(IsRootActivityRecorded("MongoDB.Driver"));
        }
    }

    [TestMethod]
    public void MongoDbActivitySourceName_IsTheDriverSource()
    {
        Assert.AreEqual(MongoTelemetry.ActivitySourceName, TelemetrySettings.MongoDbActivitySourceName);
    }

    [TestMethod]
    public void AzureSdk_ByDefault_IsNotListenedTo()
    {
        var (host, _) = BuildHost(new() { ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString });
        using (host)
        {
            Assert.IsFalse(IsRootActivityRecorded("Azure.Storage.Blobs.BlobContainerClient"));
        }
    }

    [TestMethod]
    public void AzureSdkEnabled_ListensToItsSourcesAndTurnsOnTheSwitch()
    {
        var (host, _) = BuildHost(new() {
            ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString,
            ["Telemetry:Instrumentation:AzureSdk"] = "true"
        });
        try
        {
            using (host)
            {
                Assert.IsTrue(IsRootActivityRecorded("Azure.Storage.Blobs.BlobContainerClient"));
                Assert.IsTrue(AppContext.TryGetSwitch("Azure.Experimental.EnableActivitySource", out var enabled) && enabled);
            }
        }
        finally
        {
            //the switch is process-wide - left on it would leak into the other tests
            AppContext.SetSwitch(TelemetrySettings.AzureSdkActivitySourceSwitch, false);
        }
    }

    [TestMethod]
    public async Task HttpClient_UnderAzureSdkHttpSpan_IsNotRecorded()
    {
        //the Azure SDK span has to be recorded itself - under an unsampled parent the child is dropped anyway
        var (host, _) = BuildHost(new() {
            ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString,
            ["Telemetry:Instrumentation:AzureSdk"] = "true"
        });
        try
        {
            using (host)
            {
                Assert.IsFalse(await IsHttpClientActivityRecorded(TelemetrySettings.AzureSdkHttpActivitySourceName));
            }
        }
        finally
        {
            AppContext.SetSwitch(TelemetrySettings.AzureSdkActivitySourceSwitch, false);
        }
    }

    [TestMethod]
    public async Task HttpClient_OutsideAzureSdk_IsRecorded()
    {
        var (host, _) = BuildHost(new() { ["Telemetry:AzureMonitor:ConnectionString"] = AzureConnectionString });
        using (host)
        {
            Assert.IsTrue(await IsHttpClientActivityRecorded(null));
        }
    }

    private static async Task<bool> IsHttpClientActivityRecorded(string? parentSourceName)
    {
        Activity? httpActivity = null;
        //only observes - the sampling decision stays with the pipeline
        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == "System.Net.Http" || source.Name == parentSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.PropagationData,
            ActivityStopped = activity => {
                if (activity.Source.Name == "System.Net.Http")
                    httpActivity = activity;
            }
        };
        ActivitySource.AddActivityListener(listener);

        using var parentSource = parentSourceName is null ? null : new ActivitySource(parentSourceName);
        using var parent = parentSource?.StartActivity("parent");
        //fails before any connection is made - only the client span is wanted
        using var client = new HttpClient(new SocketsHttpHandler {
            ConnectCallback = (_, _) => throw new HttpRequestException("offline")
        });
        try
        {
            await client.GetAsync("http://127.0.0.1:9/");
        }
        catch (HttpRequestException)
        {
        }

        Assert.IsNotNull(httpActivity);
        return httpActivity.Recorded;
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

    [TestMethod]
    public void OtlpEndpointInEnvironment_KeepsProtocolButNotHeadersFromFile()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://otlp.vendor:4318",
            ["Telemetry:Otlp:Endpoint"] = "http://collector:4318",
            ["Telemetry:Otlp:Protocol"] = "http/protobuf",
            ["Telemetry:Otlp:Headers"] = "x-api-key=from-file"
        });

        builder.ConfigureOpenTelemetry();

        Assert.AreEqual("https://otlp.vendor:4318", builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        Assert.AreEqual("http/protobuf", builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]);
        Assert.IsNull(builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]);
    }
}
