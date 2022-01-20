using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationUpgradeDbVersion_11 : IMigration
    {

        public int Priority => 0;

        public DbVersion Version => new(1, 1);

        public Guid Identity => new("6BDB7093-4C31-4D78-9604-58188DF728D3");

        public string Name => "Upgrade version of the database to 1.1";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<GrandNodeVersion>>();

            var dbversion = repository.Table.ToList().FirstOrDefault();
            dbversion.DataBaseVersion = $"{GrandVersion.SupportedDBVersion}";
            repository.Update(dbversion);

            return true;
        }
    }
}
