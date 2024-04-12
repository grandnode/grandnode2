using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._1;

public class MigrationUpgradeDbVersion_21 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(2, 1);

    public Guid Identity => new("EA674DAA-66B2-4F21-9C68-008ACE752FBD");

    public string Name => "Upgrade version of the database to 2.1";

    /// <summary>
    ///     Upgrade process
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