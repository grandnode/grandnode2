using Grand.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Grand.Module.Migration.Startup;

/// <summary>
///     Runs the migration process in the host's starting phase. Every StartingAsync completes before
///     any hosted service's StartAsync, so the database is migrated before Kestrel accepts a request
///     and before the scheduled tasks start.
/// </summary>
public class MigrationHostedService : IHostedLifecycleService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IFeatureManager _featureManager;

    public MigrationHostedService(IServiceScopeFactory serviceScopeFactory, IFeatureManager featureManager)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _featureManager = featureManager;
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;
        if (!await _featureManager.IsEnabledAsync("Grand.Module.Migration"))
            return;

        //IMigrationProcess is scoped - the scope ends with the migration run
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var migrationProcess = scope.ServiceProvider.GetRequiredService<IMigrationProcess>();
        await migrationProcess.RunMigrationProcess();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
