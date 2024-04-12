using Grand.Data;
using Grand.Domain;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations;

public class MigrationProcess : IMigrationProcess
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<MigrationProcess> _logger;

    private readonly IRepository<MigrationDb> _repositoryMigration;
    private readonly IServiceProvider _serviceProvider;

    public MigrationProcess(
        IDatabaseContext databaseContext,
        IServiceProvider serviceProvider,
        ILogger<MigrationProcess> logger,
        IRepository<MigrationDb> repositoryMigration)
    {
        _databaseContext = databaseContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _repositoryMigration = repositoryMigration;
    }

    public virtual MigrationResult RunProcess(IMigration migration)
    {
        var result = RunProcessInternal(migration);
        try
        {
            if (result.Success)
            {
                SaveMigration(result);
                _logger.LogInformation(
                    $"The migration of {migration.Name} ({migration.Version}) has been completed successfully.");
            }
            else
            {
                _logger.LogError("Something went wrong during migration process {MigrationName}", migration.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Exception run migration {migration.Name}", ex);
        }
    }

    public virtual void RunMigrationProcess()
    {
        var migrationsDb = GetMigrationDb();
        var migrationManager = new MigrationManager();
        foreach (var item in migrationManager.GetCurrentMigrations())
            if (migrationsDb.FirstOrDefault(x => x.Identity == item.Identity) == null)
                RunProcess(item);
    }

    public virtual void InstallApplication()
    {
        var migrationManager = new MigrationManager();
        foreach (var item in migrationManager.GetCurrentMigrations())
            SaveMigration(new MigrationResult {
                Migration = item,
                Success = true
            }, true);
    }

    private MigrationResult RunProcessInternal(IMigration migration)
    {
        var model = new MigrationResult {
            Success = migration.UpgradeProcess(_databaseContext, _serviceProvider),
            Migration = migration
        };
        return model;
    }

    private void SaveMigration(MigrationResult migrationResult, bool install = false)
    {
        _repositoryMigration.Insert(new MigrationDb {
            Identity = migrationResult.Migration.Identity,
            Name = migrationResult.Migration.Name,
            Version = migrationResult.Migration.Version.ToString(),
            InstallApp = install
        });
    }

    private IList<MigrationDb> GetMigrationDb()
    {
        return _repositoryMigration.Table.ToList();
    }
}