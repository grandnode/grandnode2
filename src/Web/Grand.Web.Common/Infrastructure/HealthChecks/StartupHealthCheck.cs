using Grand.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Grand.Web.Common.Infrastructure.HealthChecks;

/// <summary>
///     Reports whether the application has finished starting and is configured with a database
///     connection. Intentionally does not probe MongoDB or Redis - readiness here covers only the
///     application process itself. Dependency probing (DB/Redis ping) is a deliberate future
///     extension, not an oversight.
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public StartupHealthCheck(IHostApplicationLifetime applicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        _applicationLifetime = applicationLifetime;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_applicationLifetime.ApplicationStarted.IsCancellationRequested)
            return Task.FromResult(HealthCheckResult.Unhealthy("Application has not finished starting."));

        return Task.FromResult(DataSettingsManager.DatabaseIsInstalled()
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database connection is not configured."));
    }
}
