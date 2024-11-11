using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Migration.Migrations._2._3;

public class MigrationUpgradeDbVersion_23 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(2, 3);

    public Guid Identity => new("689E5BFA-7229-41A5-AF48-07CB58C0D608");

    public string Name => "Upgrade version of the database to 2.3";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<GrandNodeVersion>>();

        var dbversion = repository.Table.FirstOrDefault();
        dbversion!.InstalledVersion = $"{GrandVersion.SupportedDBVersion}";
        dbversion!.DataBaseVersion = $"{GrandVersion.SupportedDBVersion}";
        repository.Update(dbversion);

        return true;
    }
}