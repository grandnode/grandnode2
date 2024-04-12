using Grand.Data;

namespace Grand.Infrastructure.Migrations;

public interface IMigration : IBaseMigration
{
    /// <summary>
    ///     Gets order of this startup migration implementation
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider);
}