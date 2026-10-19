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
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    Task<bool> UpgradeProcess(IServiceProvider serviceProvider);
}