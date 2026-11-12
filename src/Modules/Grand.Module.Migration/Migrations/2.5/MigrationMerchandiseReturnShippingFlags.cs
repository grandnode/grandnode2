using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Marks the reasons and actions the installer seeded - by their English name, as installed - with the flags the
///     return shipping uses: a wrong or faulty product is returned at the store's cost, and a repair or replacement
///     needs no refund. A renamed or translated-only entity is left to the store owner. The settings of the return
///     shipping need no row: a missing one loads as the defaults, with no return method active
/// </summary>
public class MigrationMerchandiseReturnShippingFlags : IMigration
{
    private static readonly string[] FreeReturnShippingReasons =
        ["Received Wrong Product", "There Was A Problem With The Product"];

    private static readonly string[] WithoutRefundActions = ["Repair", "Replacement"];

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("5d2f8a61-c4e7-4b39-9a15-e0b6d3c78f24");
    public string Name => "Merchandise return: free return shipping and without refund flags 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var reasonRepository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturnReason>>();
        var actionRepository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturnAction>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnShippingFlags>>();

        try
        {
            foreach (var reason in reasonRepository.Table.ToList()
                         .Where(x => !x.FreeReturnShipping && FreeReturnShippingReasons.Contains(x.Name, StringComparer.Ordinal)))
                await reasonRepository.UpdateField(reason.Id, x => x.FreeReturnShipping, true);

            foreach (var action in actionRepository.Table.ToList()
                         .Where(x => !x.WithoutRefund && WithoutRefundActions.Contains(x.Name, StringComparer.Ordinal)))
                await actionRepository.UpdateField(action.Id, x => x.WithoutRefund, true);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnShippingFlags (2.5)");
            return false;
        }

        return true;
    }
}
