using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Migration.Migrations._2._4;

public class MigrationUpgradeDbVersion_24 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(2, 4);

    public Guid Identity => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    public string Name => "Upgrade version of the database to 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
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
