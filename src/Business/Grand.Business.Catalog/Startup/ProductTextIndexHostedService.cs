using Grand.Business.Catalog.Queries.Handlers;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Startup;

/// <summary>
///     Creates the product text index when DatabaseConfig.UseProductTextSearch is on. Runs in the host's starting
///     phase, so the index exists before Kestrel accepts a request - $text fails without it
/// </summary>
public class ProductTextIndexHostedService : IHostedLifecycleService
{
    private readonly DatabaseConfig _databaseConfig;
    private readonly ILogger<ProductTextIndexHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ProductTextIndexHostedService(
        IServiceScopeFactory serviceScopeFactory,
        DatabaseConfig databaseConfig,
        ILogger<ProductTextIndexHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _databaseConfig = databaseConfig;
        _logger = logger;
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        if (!_databaseConfig.UseProductTextSearch || _databaseConfig.UseLiteDb || !DatabaseIsInstalled())
            return;

        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
            await databaseContext.CreateTextIndex(repository, ProductTextIndex.Fields, ProductTextIndex.Name);
        }
        catch (Exception ex)
        {
            //a failed index build must not stop the store - keyword search reports the missing index instead
            _logger.LogError(ex, "Creating the product text index {IndexName} failed", ProductTextIndex.Name);
        }
    }

    protected virtual bool DatabaseIsInstalled() => DataSettingsManager.DatabaseIsInstalled();

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
