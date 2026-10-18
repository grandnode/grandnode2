using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.ServiceDefaults.Telemetry;

/// <summary>
///     Registered only when the Azure Monitor connection string still comes from ApplicationInsights:ConnectionString.
///     The key keeps working in 2.5 and is removed in 2.6.
/// </summary>
public class LegacyTelemetryConfigWarning(ILogger<LegacyTelemetryConfigWarning> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "The {LegacyKey} setting is deprecated and will be removed in GrandNode 2.6. Move the connection string to {NewKey} or the APPLICATIONINSIGHTS_CONNECTION_STRING environment variable",
            TelemetrySettings.LegacyAzureMonitorKey, $"{TelemetryConfig.SectionName}:AzureMonitor:ConnectionString");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
