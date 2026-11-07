using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.Inventory;

public class InventoryJournalService(
    IRepository<InventoryJournal> inventoryJournalRepository,
    IRepository<Product> productRepository) : IInventoryJournalService
{
    public Task<IPagedList<InventoryJournal>> GetProductHistory(string productId, string warehouseId,
        DateTime? fromUtc, DateTime? toUtc, IList<string> storeIds, int pageIndex, int pageSize)
    {
        var query = inventoryJournalRepository.Table.Where(x => x.ProductId == productId);
        if (!string.IsNullOrEmpty(warehouseId))
            query = query.Where(x => x.WarehouseId == warehouseId);
        if (fromUtc.HasValue)
            query = query.Where(x => x.CreateDateUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(x => x.CreateDateUtc <= toUtc.Value);
        //an entry written before entries named their store belongs to no store, like a shared warehouse
        if (storeIds != null)
            query = storeIds.Contains("")
                ? query.Where(x => storeIds.Contains(x.StoreId) || x.StoreId == null)
                : query.Where(x => storeIds.Contains(x.StoreId));

        return inventoryJournalRepository.PagedAsync(
            query.OrderByDescending(x => x.CreateDateUtc).ThenByDescending(x => x.StockRevision), pageIndex,
            pageSize);
    }

    public async Task<(IList<StockAtDateRow> Rows, int ProductCount)> GetStockAtDate(string warehouseId,
        DateTime atUtc, string storeId, int pageIndex, int pageSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(warehouseId);

        var query = productRepository.Table.Where(StockBuckets.KeptIn(warehouseId));
        if (storeId != null)
            query = query.Where(x => !x.LimitedToStores || x.Stores.Contains(storeId));

        var products = await productRepository.PagedAsync(query.OrderBy(x => x.Id), pageIndex, pageSize);
        var productIds = products.Select(x => x.Id).ToList();
        var entries = (await inventoryJournalRepository.ToListAsync(inventoryJournalRepository.Table.Where(x =>
                productIds.Contains(x.ProductId) && x.WarehouseId == warehouseId &&
                (x.CreateDateUtc > atUtc || x.MovementTypeId == InventoryMovementType.OpeningBalance))))
            .ToLookup(x => x.ProductId);

        return (products.SelectMany(product => StockAtDate(product, warehouseId, atUtc, entries[product.Id])).ToList(),
            products.TotalCount);
    }

    private static IEnumerable<StockAtDateRow> StockAtDate(Product product, string warehouseId, DateTime atUtc,
        IEnumerable<InventoryJournal> entries)
    {
        return StockBuckets.In(product, warehouseId).Select(bucket =>
        {
            var bucketEntries = entries.Where(x => (x.CombinationId ?? "") == bucket.CombinationId).ToList();
            //the history of a bucket starts with its latest opening balance; a bucket without one has it all
            var opening = bucketEntries.Where(x => x.MovementTypeId == InventoryMovementType.OpeningBalance)
                .Select(x => (DateTime?)x.CreateDateUtc).Max();
            var later = bucketEntries
                .Where(x => x.CreateDateUtc > atUtc && x.MovementTypeId is not (InventoryMovementType.OpeningBalance
                    or InventoryMovementType.Legacy))
                .Sum(x => x.InQty - x.OutQty);
            return new StockAtDateRow(product.Id, product.Name, product.Sku, bucket.CombinationId, bucket.Attributes,
                bucket.Stock - later, opening == null || atUtc >= opening);
        });
    }
}
