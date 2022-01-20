using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._2
{
    public class MigrationUpgradeDbVersion_20 : IMigration
    {

        public int Priority => 0;

        public DbVersion Version => new(2, 0);

        public Guid Identity => new("AEC3CF1F-4443-474A-B932-4F91D08C8F61");

        public string Name => "Upgrade version of the database to 2.0";

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
