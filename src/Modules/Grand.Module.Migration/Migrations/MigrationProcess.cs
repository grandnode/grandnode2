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

    public virtual async Task<MigrationResult> RunProcess(IMigration migration)
    {
        var result = await RunProcessInternal(migration);
        try
        {
            if (result.Success)
            {
                await SaveMigration(result);
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

    public virtual async Task RunMigrationProcess()
    {
        var migrationsDb = await GetMigrationDb();
        var version = await _repositoryVersion.FirstOrDefaultAsync(_repositoryVersion.Table);
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

        foreach (var item in GetCurrentMigrations(installedVersion))
        {
            if (migrationsDb.Any(x => x.Identity == item.Identity))
                continue;

            if ((await RunProcess(item)).Success) continue;

            //stop before the version stamp of this version runs - recording a version whose
            //migrations did not all succeed puts them below the installed version, and
            //GetCurrentMigrations never selects them again
            _logger.LogError("Migration process stopped at {MigrationName} ({Version}) - it will be retried on the next start",
                item.Name, item.Version);
            return;
        }
    }

    protected virtual IEnumerable<IMigration> GetCurrentMigrations(DbVersion installedVersion)
    {
        return new MigrationManager().GetCurrentMigrations(installedVersion);
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

    private async Task<MigrationResult> RunProcessInternal(IMigration migration)
    {
        bool success;
        try
        {
            success = await migration.UpgradeProcess(_serviceProvider);
        }
        catch (Exception ex)
        {
            //a migration that throws counts as failed - it is not recorded and runs again on the next start
            _logger.LogError(ex, "Migration {MigrationName} threw an exception", migration.Name);
            success = false;
        }

        return new MigrationResult {
            Success = success,
            Migration = migration
        };
    }

    private Task SaveMigration(MigrationResult migrationResult, bool install = false)
    {
        return _repositoryMigration.InsertAsync(new MigrationDb {
            Identity = migrationResult.Migration.Identity,
            Name = migrationResult.Migration.Name,
            Version = migrationResult.Migration.Version.ToString()
        });
    }

    private Task<IList<MigrationDb>> GetMigrationDb()
    {
        return _repositoryMigration.ToListAsync(_repositoryMigration.Table);
    }
}