using Grand.Data;
using Grand.Data.Inventory;
using Grand.Domain.Admin;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Grand.Domain.Permissions;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Batches and serial numbers: their indexes, the daily expiry e-mail (task and template) and the admin menu. The
///     installer seeds the same (the template body is repeated - a test keeps them equal). Idempotent: what exists is
///     left alone. Existing products keep tracking no units; nothing of their data changes
/// </summary>
public class MigrationInventoryUnits : IMigration
{
    private const string TaskName = "Inventory batch expiry notification";
    private const string TemplateName = "Inventory.BatchesExpiring.StoreOwnerNotification";

    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("6b1f3c8e-27d4-4a95-b0e6-9c3d1a7f5e42");
    public string Name => "Inventory batches and serial numbers: indexes, expiry e-mail, admin menu";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationInventoryUnits>>();
        var success = true;

        async Task Step(string name, Func<Task> step)
        {
            try
            {
                await step();
            }
            catch (Exception ex)
            {
                logService.LogError(ex, "UpgradeProcess - InventoryUnits {Step} (2.5)", name);
                success = false;
            }
        }

        await Step("indexes", () => InventoryIndexes.CreateUnitIndexes(
            serviceProvider.GetRequiredService<IDatabaseContext>(),
            serviceProvider.GetRequiredService<IRepository<ProductBatch>>(),
            serviceProvider.GetRequiredService<IRepository<ProductSerialNumber>>()));
        await Step("task", () => AddTask(serviceProvider));
        await Step("template", () => AddTemplate(serviceProvider));
        await Step("admin menu", () => AddSiteMap(serviceProvider));
        return success;
    }

    private static async Task AddTask(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        if (!await repository.AnyAsync(repository.Table.Where(x => x.ScheduleTaskName == TaskName)))
            await repository.InsertAsync(new ScheduleTask {
                ScheduleTaskName = TaskName, Enabled = true, StopOnError = false, TimeInterval = 1440
            });
    }

    private static async Task AddTemplate(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
        if (await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == TemplateName)) != null)
            return;

        await repository.InsertAsync(Template(await MigrationOrderApproval.GetEmailAccountId(serviceProvider)));

        //a lookup made before the upgrade may have cached "no such template"
        var cache = serviceProvider.GetService<ICacheBase>();
        if (cache != null)
            await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
    }

    /// <summary>
    ///     The template, as the installer seeds it: same name, subject and tokens
    /// </summary>
    public static MessageTemplate Template(string emailAccountId)
    {
        return new MessageTemplate {
            Name = TemplateName,
            Subject = "{{Store.Name}} — Batches expiring or expired",
            Body = MigrationOrderApproval.Layout(
                "Batches of your store are expiring or have expired.",
                "Expiring batches",
                """
                <p>These batches of {{Store.Name}} hold stock and are expiring or have expired. Nothing is blocked: decide what to do with them.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                <tr><th style='text-align: left;'>Product</th><th style='text-align: left;'>Warehouse</th><th style='text-align: left;'>Batch</th><th style='text-align: left;'>Expiry date</th><th style='text-align: right;'>Quantity</th><th style='text-align: left;'>Status</th></tr>
                {% for batch in Inventory.Batches %}
                <tr><td>{{batch.ProductName | Escape}}</td><td>{{batch.Warehouse | Escape}}</td><td>{{batch.Number | Escape}}</td><td>{{batch.ExpiryDate}}</td><td style='text-align: right;'>{{batch.Quantity}}</td><td>{{batch.Status}}</td></tr>
                {% endfor %}
                </table>
                """),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
    }

    private static async Task AddSiteMap(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var catalog = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Catalog"));
        if (catalog == null)
            return;

        var changed = false;
        foreach (var (systemName, resourceName, actionName, displayOrder) in new[] {
                     ("Batches", "Admin.Inventory.Units.Batches", "Batches", 22),
                     ("Serial numbers", "Admin.Inventory.Units.SerialNumbers", "SerialNumbers", 23)
                 })
        {
            if (catalog.ChildNodes.Any(x => x.SystemName == systemName))
                continue;

            catalog.ChildNodes.Add(new AdminSiteMap {
                SystemName = systemName,
                ResourceName = resourceName,
                PermissionNames = new List<string> { PermissionSystemName.InventoryDocuments },
                ControllerName = "ProductUnit",
                ActionName = actionName,
                DisplayOrder = displayOrder,
                IconClass = "bi bi-record-circle"
            });
            changed = true;
        }

        if (changed)
            await repository.UpdateAsync(catalog);
    }
}
