using Grand.Data;
using Grand.Domain.Admin;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     "Recurring payments" in the menu, under Sales right after Payment transactions, as the installer seeds it.
///     Idempotent: an existing entry is left alone, and a menu without the Sales section gets nothing
/// </summary>
public class MigrationRecurringPaymentMenu : IMigration
{
    private const string NodeName = "Recurring payments";

    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("5b0e7d1a-3c4f-4a8e-9f21-6d7c2e8b4a90");
    public string Name => "Recurring payments in the admin menu 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecurringPaymentMenu>>();

        try
        {
            var sales = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Sales"));
            if (sales == null || sales.ChildNodes.Any(x => x.SystemName == NodeName))
                return true;

            //right after Payment transactions, the entries below it move down by one
            var paymentTransactions = sales.ChildNodes.FirstOrDefault(x => x.SystemName == "Payment transaction");
            var displayOrder = paymentTransactions != null
                ? paymentTransactions.DisplayOrder + 1
                : sales.ChildNodes.Count == 0 ? 0 : sales.ChildNodes.Max(x => x.DisplayOrder) + 1;
            foreach (var node in sales.ChildNodes.Where(x => x.DisplayOrder >= displayOrder))
                node.DisplayOrder++;

            sales.ChildNodes.Add(new AdminSiteMap {
                SystemName = NodeName,
                ResourceName = "Admin.Orders.RecurringPayments",
                PermissionNames = new List<string> { PermissionSystemName.Orders },
                ControllerName = "RecurringPayment",
                ActionName = "List",
                DisplayOrder = displayOrder,
                IconClass = "bi bi-record-circle"
            });
            await repository.UpdateAsync(sales);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecurringPaymentMenu (2.5)");
            return false;
        }

        return true;
    }
}
