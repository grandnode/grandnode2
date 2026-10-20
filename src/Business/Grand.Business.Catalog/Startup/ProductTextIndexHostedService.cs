using Grand.Business.Catalog.Queries.Handlers;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Startup;

/// <summary>
///     Creates the product text index when DatabaseConfig.UseProductTextSearch is on. Runs in the background, so a
///     long first build on a large catalog does not hold the host's start; keyword search scans as before until the
///     index answers queries
/// </summary>
public class ProductTextIndexHostedService : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_databaseConfig.UseProductTextSearch || !DatabaseIsInstalled())
            return;

        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Product>>();
            await databaseContext.CreateTextIndex(repository, ProductTextIndex.Fields, ProductTextIndex.Name,
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            //the host is stopping - the next start creates the index
        }
        catch (Exception ex)
        {
            //a failed index build must not stop the store - keyword search keeps scanning without it
            _logger.LogError(ex, "Creating the product text index {IndexName} failed", ProductTextIndex.Name);
        }
    }

    protected virtual bool DatabaseIsInstalled() => DataSettingsManager.DatabaseIsInstalled();
}
