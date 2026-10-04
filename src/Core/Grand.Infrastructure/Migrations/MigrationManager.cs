using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearch;

namespace Grand.Infrastructure.Migrations;

public class MigrationManager
{
    private readonly IEnumerable<Type> _migrationConfigurations;

    public MigrationManager()
    {
        var typeSearcher = new TypeSearcher();
        _migrationConfigurations = typeSearcher.ClassesOfType<IMigration>();
    }

    /// <summary>
    ///     Get all migrations
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IMigration> GetAllMigrations()
    {
        return _migrationConfigurations
            .Where(PluginExtensions.OnlyInstalledPlugins)
            .Select(mg => (IMigration)Activator.CreateInstance(mg))
            .OrderBy(mg => mg!.Priority);
    }

    /// <summary>
    ///     Get current migrations
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IMigration> GetCurrentMigrations(DbVersion installedVersion)
    {
        return GetAllMigrations()
            .Where(x => x.Version.CompareTo(installedVersion) > 0)
            //DbVersion, not its string form - "2.10" sorts before "2.2" as text
            .OrderBy(mg => mg.Version)
            //the version stamp closes its version, whatever priority it declares
            .ThenBy(mg => mg is IMigrationVersionStamp ? 1 : 0)
            .ThenBy(mg => mg.Priority)
            .ToList();
    }
}