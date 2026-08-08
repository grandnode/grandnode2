using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Migration.Migrations;

/// <summary>
///     Records <see cref="Version" /> as the version the database has reached.
/// </summary>
/// <remarks>
///     Runs last within its version - see <see cref="IMigrationVersionStamp" />.
/// </remarks>
public abstract class MigrationUpgradeDbVersionBase : IMigrationVersionStamp
{
    public int Priority => 0;

    public abstract DbVersion Version { get; }

    public abstract Guid Identity { get; }

    public string Name => $"Upgrade version of the database to {Version}";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<GrandNodeVersion>>();

        var dbversion = repository.Table.FirstOrDefault();
        if (dbversion == null)
            return false;

        //stamp the version this migration closes, not the version the build supports - stamping
        //the latest supported version here makes every migration in between unreachable
        dbversion.InstalledVersion = Version.ToString();
        dbversion.DataBaseVersion = Version.ToString();
        repository.Update(dbversion);

        return true;
    }
}
