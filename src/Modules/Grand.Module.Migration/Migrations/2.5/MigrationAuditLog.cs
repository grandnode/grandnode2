using Grand.Data;
using Grand.Data.Audit;
using Grand.Domain.Admin;
using Grand.Domain.Audit;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationAuditLog : IMigration
{
    private const string TaskName = "Audit log retention";

    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("8d2f4b6a-1e3c-4f7a-9b0d-6c5e2a8f1d47");
    public string Name => "Audit log: indexes, retention task, permission and admin menu";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var auditRepository = serviceProvider.GetRequiredService<IRepository<AuditEntry>>();
        var taskRepository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationAuditLog>>();
        var success = true;

        try
        {
            await AuditIndexes.Create(databaseContext, auditRepository);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AuditLog indexes (2.5)");
            success = false;
        }

        try
        {
            if (!await taskRepository.AnyAsync(taskRepository.Table.Where(x => x.ScheduleTaskName == TaskName)))
                await taskRepository.InsertAsync(new ScheduleTask {
                    ScheduleTaskName = TaskName, Enabled = true, StopOnError = false, TimeInterval = 1440
                });
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AuditLog retention task (2.5)");
            success = false;
        }

        try
        {
            await AddPermission(serviceProvider);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AuditLog permission (2.5)");
            success = false;
        }

        try
        {
            await AddSiteMap(serviceProvider);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AuditLog admin menu (2.5)");
            success = false;
        }

        return success;
    }

    private static async Task AddPermission(IServiceProvider serviceProvider)
    {
        var permissionRepository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var groupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();

        if (await permissionRepository.AnyAsync(
                permissionRepository.Table.Where(x => x.SystemName == PermissionSystemName.AuditLog)))
            return;

        var permission = new Permission {
            Name = StandardPermission.ManageAuditLog.Name,
            SystemName = StandardPermission.ManageAuditLog.SystemName,
            Area = StandardPermission.ManageAuditLog.Area,
            Category = StandardPermission.ManageAuditLog.Category,
            Actions = StandardPermission.ManageAuditLog.Actions.ToList()
        };
        var administrators = await groupRepository.FirstOrDefaultAsync(
            groupRepository.Table.Where(x => x.SystemName == SystemCustomerGroupNames.Administrators));
        if (administrators != null)
            permission.CustomerGroups.Add(administrators.Id);

        await permissionRepository.InsertAsync(permission);
    }

    private static async Task AddSiteMap(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var system = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "System"));
        if (system == null || system.ChildNodes.Any(x => x.SystemName == "Audit log"))
            return;

        system.ChildNodes.Add(new AdminSiteMap {
            SystemName = "Audit log",
            ResourceName = "Admin.System.AuditLog",
            PermissionNames = new List<string> { PermissionSystemName.AuditLog },
            ControllerName = "AuditLog",
            ActionName = "List",
            DisplayOrder = 6,
            IconClass = "bi bi-record-circle"
        });
        if (!system.PermissionNames.Contains(PermissionSystemName.AuditLog))
            system.PermissionNames.Add(PermissionSystemName.AuditLog);
        await repository.UpdateAsync(system);
    }
}
