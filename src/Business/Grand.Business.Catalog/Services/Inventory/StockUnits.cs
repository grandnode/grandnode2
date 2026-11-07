using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Catalog.Services.Inventory;

/// <summary>
///     One stock bucket as the units see it: the combination, the warehouse the journal names, and its stock before the
///     change at hand
/// </summary>
public sealed record UnitBucket(string CombinationId, IList<CustomAttribute> Attributes, string WarehouseId, int Stock)
{
    internal (string, string) Key => (CombinationId ?? "", WarehouseId ?? "");
}

/// <summary>
///     The batches or serial numbers of one product a call of the ledger reads and changes, in memory until written
/// </summary>
public sealed class UnitSet
{
    internal UnitSet(InventoryTrackingMode mode)
    {
        Mode = mode;
    }

    internal InventoryTrackingMode Mode { get; }
    internal List<ProductBatch> Batches { get; } = [];
    internal HashSet<object> Changed { get; } = new(ReferenceEqualityComparer.Instance);
    internal HashSet<object> Added { get; } = new(ReferenceEqualityComparer.Instance);
    internal Dictionary<StockChange, List<object>> Touched { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     The stock without units of each bucket before the call changed it
    /// </summary>
    internal Dictionary<(string, string), int> InitialPool { get; } = new();

    /// <summary>
    ///     The buckets a shipment took stock without units from while the product allows backorders
    /// </summary>
    internal HashSet<(string, string)> Backordered { get; } = [];
}

/// <summary>
///     Applies the units a stock change names, its batches or serial numbers, for the <see cref="StockLedger" />, under
///     the product's lock. A change names units for part of its quantity or all of it; the rest is stock without
///     units, the pool: a bucket's stock less its assigned units. Records are written after the journal entries and
///     before the product, each stamped with the revision of the last entry that changed it, so a lost write is applied
///     again once and never twice
/// </summary>
public sealed class StockUnits(
    IRepository<ProductBatch> batchRepository,
    IRepository<ProductSerialNumber> serialNumberRepository,
    IRepository<Shipment> shipmentRepository,
    IWarehouseService warehouseService)
{
    /// <summary>
    ///     Reads the units the changes and the entries to repair name, and every batch of the product holding stock
    /// </summary>
    public async Task<UnitSet> Load(Product product, IEnumerable<StockChange> changes,
        IEnumerable<InventoryJournal> pending)
    {
        ArgumentNullException.ThrowIfNull(product);

        var set = new UnitSet(product.InventoryTrackingModeId);
        if (set.Mode == InventoryTrackingMode.Batch)
        {
            var keys = changes.Select(x => x.BatchNumber).Concat(pending.Select(x => x.BatchNumber))
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(UnitNumbers.Key).Distinct().ToList();
            set.Batches.AddRange(await batchRepository.ToListAsync(batchRepository.Table.Where(x =>
                x.ProductId == product.Id && (x.Quantity != 0 || keys.Contains(x.NumberKey)))));
        }

        return set;
    }

    /// <summary>
    ///     Applies the units of one change to the records read, before the change moves the bucket's stock
    /// </summary>
    /// <param name="set">The units read for the call</param>
    /// <param name="product">The product, read under its lock</param>
    /// <param name="bucket">The bucket, with its stock before the change</param>
    /// <param name="delta">The change of the bucket's stock; 0 for an assignment</param>
    /// <param name="change">The change</param>
    /// <returns>The key of the error, or null</returns>
    public async Task<string> Apply(UnitSet set, Product product, UnitBucket bucket, int delta, StockChange change)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(bucket);
        ArgumentNullException.ThrowIfNull(change);

        set.InitialPool.TryAdd(bucket.Key, bucket.Stock - Assigned(set, bucket));

        return set.Mode == InventoryTrackingMode.Batch ? await ApplyBatch(set, product, bucket, delta, change) : null;
    }

    /// <summary>
    ///     Whether the pool of every bucket the call changed is still covered: a call never takes a pool below zero,
    ///     except a shipment of a batch-tracked product that allows backorders
    /// </summary>
    /// <param name="set">The units read for the call</param>
    /// <param name="buckets">The buckets the call changed, with their stock after it</param>
    /// <returns>The key of the error, or null</returns>
    public Task<string> CheckPools(UnitSet set, IEnumerable<UnitBucket> buckets)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(buckets);

        foreach (var bucket in buckets)
        {
            var pool = bucket.Stock - Assigned(set, bucket);
            if (pool < 0 && pool < set.InitialPool.GetValueOrDefault(bucket.Key) &&
                !(set.Mode == InventoryTrackingMode.Batch && set.Backordered.Contains(bucket.Key)))
                return Task.FromResult(UnitErrors.PoolExceeded);
        }

        return Task.FromResult<string>(null);
    }

    /// <summary>
    ///     Stamps the records the change touched with the revision of its journal entry
    /// </summary>
    public void Stamp(UnitSet set, StockChange change, InventoryJournal entry)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(entry);

        if (!set.Touched.TryGetValue(change, out var records))
            return;

        foreach (var record in records)
            switch (record)
            {
                case ProductBatch batch: batch.StockRevision = entry.StockRevision; break;
                case ProductSerialNumber serialNumber: serialNumber.StockRevision = entry.StockRevision; break;
            }
    }

    /// <summary>
    ///     Writes the records the call added or changed
    /// </summary>
    public async Task Write(UnitSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        foreach (var record in set.Changed)
            switch (record)
            {
                case ProductBatch batch when set.Added.Contains(batch):
                    await batchRepository.InsertAsync(batch);
                    break;
                case ProductBatch batch:
                    await batchRepository.UpdateAsync(batch);
                    break;
            }
    }

    private static int Assigned(UnitSet set, UnitBucket bucket)
    {
        return set.Mode == InventoryTrackingMode.Batch
            ? set.Batches.Where(x => InBucket(x.CombinationId, x.WarehouseId, bucket)).Sum(x => x.Quantity)
            : 0;
    }

    private static bool InBucket(string combinationId, string warehouseId, UnitBucket bucket)
    {
        return (combinationId ?? "") == (bucket.CombinationId ?? "") && (warehouseId ?? "") == (bucket.WarehouseId ?? "");
    }

    private static void Touch(UnitSet set, StockChange change, object record)
    {
        if (!set.Touched.TryGetValue(change, out var records))
            set.Touched[change] = records = [];
        records.Add(record);
        set.Changed.Add(record);
    }

    private async Task<string> ApplyBatch(UnitSet set, Product product, UnitBucket bucket, int delta,
        StockChange change)
    {
        var key = UnitNumbers.Key(change.BatchNumber);
        if (change.MovementType == InventoryMovementType.TrackingAssignment)
        {
            if (key == "")
                return UnitErrors.BatchRequired;
            if (change.Quantity <= 0)
                return UnitErrors.BatchQuantityExceeded;

            var (assigned, error) = await Receive(set, product, bucket, key, change);
            if (error != null)
                return error;

            assigned.Quantity += change.Quantity;
            Touch(set, change, assigned);
            return null;
        }

        if (key == "")
        {
            if (change.MovementType == InventoryMovementType.Shipment && delta < 0 &&
                product.BackorderModeId != BackorderMode.NoBackorders)
                set.Backordered.Add(bucket.Key);
            return null;
        }

        if (delta > 0)
        {
            var (received, error) = await Receive(set, product, bucket, key, change);
            if (error != null)
                return error;

            received.Quantity += delta;
            Touch(set, change, received);
        }
        else if (delta < 0)
        {
            var batch = Find(set, bucket, key);
            if (batch == null)
                return UnitErrors.BatchNotFound;
            if (batch.Quantity < -delta)
                return UnitErrors.BatchQuantityExceeded;

            batch.Quantity += delta;
            Touch(set, change, batch);
        }

        return null;
    }

    private static ProductBatch Find(UnitSet set, UnitBucket bucket, string key)
    {
        return set.Batches.FirstOrDefault(x => x.NumberKey == key && InBucket(x.CombinationId, x.WarehouseId, bucket));
    }

    /// <summary>
    ///     The batch of the bucket that takes stock in; a batch new in the bucket keeps the expiry date, cost and
    ///     receipt the number has elsewhere, and only a batch new for the product takes them from the change
    /// </summary>
    private async Task<(ProductBatch Batch, string Error)> Receive(UnitSet set, Product product, UnitBucket bucket,
        string key, StockChange change)
    {
        var batch = Find(set, bucket, key);
        var known = batch ?? set.Batches.FirstOrDefault(x =>
            x.NumberKey == key && (x.CombinationId ?? "") == (bucket.CombinationId ?? ""));
        if (change.ExpiryDateUtc.HasValue && known != null &&
            known.ExpiryDateUtc?.Date != change.ExpiryDateUtc.Value.Date)
            return (null, UnitErrors.BatchExpiryMismatch);
        if (batch != null)
            return (batch, null);

        batch = new ProductBatch {
            ProductId = product.Id,
            CombinationId = bucket.CombinationId ?? "",
            Attributes = bucket.Attributes ?? [],
            WarehouseId = bucket.WarehouseId ?? "",
            StoreId = await StoreOf(bucket.WarehouseId),
            Number = known?.Number ?? change.BatchNumber.Trim(),
            NumberKey = key,
            ExpiryDateUtc = known != null ? known.ExpiryDateUtc : Date(change.ExpiryDateUtc),
            UnitCost = known != null ? known.UnitCost : change.UnitCost,
            ReceivedOnUtc = DateTime.UtcNow,
            ReceiptDocumentId = known != null ? known.ReceiptDocumentId : ReceiptDocument(change)
        };
        set.Batches.Add(batch);
        set.Added.Add(batch);
        return (batch, null);
    }

    private static DateTime? Date(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc) : null;
    }

    private static string ReceiptDocument(StockChange change)
    {
        return change.MovementType == InventoryMovementType.Receipt &&
               change.Source.ObjectType == nameof(InventoryDocument)
            ? change.Source.ObjectId
            : null;
    }

    private async Task<string> StoreOf(string warehouseId)
    {
        return string.IsNullOrEmpty(warehouseId)
            ? ""
            : (await warehouseService.GetWarehouseById(warehouseId))?.StoreId ?? "";
    }
}
