using Grand.Data;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._1;

public class MigrationSystemPermission : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 1);
    public Guid Identity => new("D5FA1ACE-34A5-4CE4-A4CE-6A6DE2CEFFD2");
    public string Name => "Remove old permissions";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationSystemPermission>>();
        try
        {
            var permissionManageActions = repository.Table.FirstOrDefault(x => x.SystemName == "ManageActions");
            if (permissionManageActions != null) repository.Delete(permissionManageActions);
            var permissionManageReminders = repository.Table.FirstOrDefault(x => x.SystemName == "ManageReminders");
            if (permissionManageReminders != null) repository.Delete(permissionManageReminders);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RemoveOldPermissions");
        }

        return true;
    }
}