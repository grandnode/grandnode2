using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Business.System.Tests.Services.Migrations
{
    public class MigrationUpgrade_Test : IMigration
    {

        public int Priority => 0;

        public DbVersion Version => new(int.Parse(Grand.Infrastructure.GrandVersion.MajorVersion), int.Parse(Grand.Infrastructure.GrandVersion.MinorVersion));

        public Guid Identity => new("6BDB7093-4C31-4D78-9604-58188DF728D3");

        public string Name => "Upgrade database";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        { 
            return true;
        }
    }
}
