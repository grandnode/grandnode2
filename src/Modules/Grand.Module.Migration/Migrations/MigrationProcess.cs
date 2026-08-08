using Grand.Data;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations;

public class MigrationProcess : IMigrationProcess
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<MigrationProcess> _logger;

    private readonly IRepository<MigrationDb> _repositoryMigration;
    private readonly IRepository<GrandNodeVersion> _repositoryVersion;
    private readonly IServiceProvider _serviceProvider;

    public MigrationProcess(
        IDatabaseContext databaseContext,
        IServiceProvider serviceProvider,
        ILogger<MigrationProcess> logger,
        IRepository<MigrationDb> repositoryMigration,
        IRepository<GrandNodeVersion> repositoryVersion)
    {
        _databaseContext = databaseContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _repositoryMigration = repositoryMigration;
        _repositoryVersion = repositoryVersion;
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
        var version = _repositoryVersion.Table.FirstOrDefault();
        if (version == null)
            return;

        var installedVersion = ParseDbVersion(string.IsNullOrEmpty(version.InstalledVersion)
            ? version.DataBaseVersion
            : version.InstalledVersion);

        if (installedVersion == null)
        {
            _logger.LogError("Cannot read the installed database version - migration process skipped");
            return;
        }

        var migrationManager = new MigrationManager();
        foreach (var item in migrationManager.GetCurrentMigrations(installedVersion))
        {
            if (migrationsDb.Any(x => x.Identity == item.Identity))
                continue;

            if (RunProcess(item).Success) continue;

            //stop before the version stamp of this version runs - recording a version whose
            //migrations did not all succeed puts them below the installed version, and
            //GetCurrentMigrations never selects them again
            _logger.LogError("Migration process stopped at {MigrationName} ({Version}) - it will be retried on the next start",
                item.Name, item.Version);
            return;
        }
    }

    private static DbVersion? ParseDbVersion(string? version)
    {
        var parts = version?.Split('.');
        if (parts is not { Length: >= 2 })
            return null;

        return int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor)
            ? new DbVersion(major, minor)
            : null;
    }

    private MigrationResult RunProcessInternal(IMigration migration)
    {
        var model = new MigrationResult {
            Success = migration.UpgradeProcess(_serviceProvider),
            Migration = migration
        };
        return model;
    }

    private void SaveMigration(MigrationResult migrationResult, bool install = false)
    {
        _repositoryMigration.Insert(new MigrationDb {
            Identity = migrationResult.Migration.Identity,
            Name = migrationResult.Migration.Name,
            Version = migrationResult.Migration.Version.ToString()
        });
    }

    private IList<MigrationDb> GetMigrationDb()
    {
        return _repositoryMigration.Table.ToList();
    }
}