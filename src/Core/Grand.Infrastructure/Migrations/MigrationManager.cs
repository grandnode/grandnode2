using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;

namespace Grand.Infrastructure.Migrations
{
    public class MigrationManager
    {
        private readonly IEnumerable<Type> _migrationConfigurations;

        public MigrationManager()
        {
            var typeSearcher = new AppTypeSearcher();
            _migrationConfigurations = typeSearcher.ClassesOfType<IMigration>();
        }
        
        /// <summary>
        /// Get all migrations
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IMigration> GetAllMigrations()
        {
            return _migrationConfigurations
                .Where(mg => PluginExtensions.OnlyInstalledPlugins(mg))
                .Select(mg => (IMigration)Activator.CreateInstance(mg))
                .OrderBy(mg => mg.Priority);
        }

        /// <summary>
        /// Get current migrations 
        /// </summary>
        /// <param name="dbVersion"></param>
        /// <returns></returns>
        public IEnumerable<IMigration> GetCurrentMigrations()
        {
            var currentDbVersion = new DbVersion(int.Parse(GrandVersion.MajorVersion), int.Parse(GrandVersion.MinorVersion));

            return GetAllMigrations()
                .Where(x => currentDbVersion.CompareTo(x.Version) >= 0)
                .OrderBy(mg => mg.Version.ToString())
                .ThenBy(mg => mg.Priority)
                .ToList();
        }
    }
}
