using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._2;

public class MigrationUpgradeDbVersion_22 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(2, 2);

    public Guid Identity => new("9B9FD138-7E67-44AA-913B-273F3D5B5DE9");

    public string Name => "Upgrade version of the database to 2.2";

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