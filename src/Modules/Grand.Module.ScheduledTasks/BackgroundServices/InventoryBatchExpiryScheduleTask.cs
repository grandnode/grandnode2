using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     E-mails each store owner the batches of the store's warehouses that became expiring or expired since the last
///     run; batches of shared warehouses go to the first store. Nothing is blocked or written off
/// </summary>
public class InventoryBatchExpiryScheduleTask(
    IProductUnitService productUnitService,
    IStoreService storeService,
    ICatalogMessageProviderService messageProviderService,
    IDateTimeService dateTimeService) : IScheduleTask
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        var today = dateTimeService.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc).Date;
        var batches = await productUnitService.BatchesToNotify(today);
        if (batches.Count == 0)
            return;

        var stores = (await storeService.GetAllStores()).OrderBy(x => x.DisplayOrder).ToList();
        if (stores.Count == 0)
            return;

        foreach (var storeBatches in batches.GroupBy(x =>
                     stores.FirstOrDefault(store => store.Id == x.Batch.StoreId) ?? stores[0]))
        {
            cancellationToken.ThrowIfCancellationRequested();

            //marked only once sent: without the message template they are sent when it exists
            var list = storeBatches.ToList();
            if (await messageProviderService.SendBatchesExpiringStoreOwnerMessage(storeBatches.Key, list,
                    storeBatches.Key.DefaultLanguageId) > 0)
                await productUnitService.MarkNotified(list);
        }
    }
}
