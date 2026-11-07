using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.Inventory;

public class ProductUnitService(
    IRepository<ProductBatch> batchRepository,
    IRepository<ProductSerialNumber> serialNumberRepository) : IProductUnitService
{
    public async Task<int> AssignedQuantity(string productId, string combinationId = null)
    {
        var batches = batchRepository.Table.Where(x => x.ProductId == productId && x.Quantity != 0);
        var serialNumbers = serialNumberRepository.Table.Where(x =>
            x.ProductId == productId && x.StatusId == SerialNumberStatus.InStock);
        if (combinationId != null)
        {
            batches = batches.Where(x => x.CombinationId == combinationId);
            serialNumbers = serialNumbers.Where(x => x.CombinationId == combinationId);
        }

        return await Assigned(batches, serialNumbers);
    }

    public Task<int> AssignedInWarehouse(string warehouseId)
    {
        return Assigned(batchRepository.Table.Where(x => x.WarehouseId == warehouseId && x.Quantity != 0),
            serialNumberRepository.Table.Where(x =>
                x.WarehouseId == warehouseId && x.StatusId == SerialNumberStatus.InStock));
    }

    public async Task<IList<ProductBatch>> Batches(string productId, string combinationId, string warehouseId)
    {
        combinationId ??= "";
        warehouseId ??= "";
        var batches = await batchRepository.ToListAsync(batchRepository.Table.Where(x =>
            x.ProductId == productId && x.CombinationId == combinationId && x.WarehouseId == warehouseId &&
            x.Quantity != 0));
        return batches.OrderBy(x => x.ExpiryDateUtc == null).ThenBy(x => x.ExpiryDateUtc).ThenBy(x => x.Number)
            .ToList();
    }

    public async Task<string> TrackingChangeRefused(Product stored, Product changed)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(changed);

        if (changed.InventoryTrackingModeId != stored.InventoryTrackingModeId &&
            changed.InventoryTrackingModeId != InventoryTrackingMode.None && !InventoryTracking.Tracked(changed))
            return UnitErrors.TrackingNotSupported;

        //switching tracking on keeps the stock as stock without units: only a change of what units are or where
        //they are kept is blocked while there are some
        var modeChanged = changed.InventoryTrackingModeId != stored.InventoryTrackingModeId &&
                          stored.InventoryTrackingModeId != InventoryTrackingMode.None;
        var bucketsChanged = changed.ManageInventoryMethodId != stored.ManageInventoryMethodId ||
                             changed.UseMultipleWarehouses != stored.UseMultipleWarehouses ||
                             (!changed.UseMultipleWarehouses &&
                              (changed.WarehouseId ?? "") != (stored.WarehouseId ?? ""));
        if (stored.InventoryTrackingModeId == InventoryTrackingMode.None || (!modeChanged && !bucketsChanged))
            return null;

        return await AssignedQuantity(stored.Id) > 0 ? UnitErrors.UnitsAssigned : null;
    }

    private async Task<int> Assigned(IQueryable<ProductBatch> batches, IQueryable<ProductSerialNumber> serialNumbers)
    {
        var quantities = await batchRepository.ToListAsync(batches.Select(x => x.Quantity));
        return quantities.Sum(Math.Abs) + await serialNumberRepository.CountAsync(serialNumbers);
    }
}
