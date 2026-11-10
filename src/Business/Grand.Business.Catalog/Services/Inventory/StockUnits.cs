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

    /// <summary>
    ///     The serial numbers read, by key
    /// </summary>
    internal Dictionary<string, ProductSerialNumber> SerialNumbers { get; } = new();

    /// <summary>
    ///     The serial numbers in stock in each bucket the call touched
    /// </summary>
    internal Dictionary<(string, string), int> InStock { get; } = new();

    /// <summary>
    ///     The serial numbers a transfer took out of their warehouse and has not put in the other yet
    /// </summary>
    internal HashSet<string> InTransit { get; } = [];
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
        else if (set.Mode == InventoryTrackingMode.SerialNumber)
        {
            var keys = changes.SelectMany(x => x.SerialNumbers.Concat(x.CountedSerialNumbers ?? []))
                .Concat(pending.SelectMany(x => x.SerialNumbers ?? []))
                .Select(UnitNumbers.Key).Where(x => x != "").Distinct().ToList();
            if (keys.Count > 0)
                foreach (var serialNumber in await serialNumberRepository.ToListAsync(serialNumberRepository.Table
                             .Where(x => x.ProductId == product.Id && keys.Contains(x.NumberKey))))
                    set.SerialNumbers[serialNumber.NumberKey] = serialNumber;
        }

        return set;
    }

    /// <summary>
    ///     A shipment reversal gives back only the serial numbers still shipped under its shipment: one returned since,
    ///     or returned and shipped again, came back to stock with its return, so it is left out with its unit
    /// </summary>
    public IReadOnlyList<StockChange> StillShipped(UnitSet set, IReadOnlyList<StockChange> changes)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(changes);

        if (set.Mode != InventoryTrackingMode.SerialNumber ||
            changes.All(x => x.MovementType != InventoryMovementType.ShipmentReversal))
            return changes;

        var kept = new List<StockChange>();
        foreach (var change in changes)
        {
            if (change.MovementType != InventoryMovementType.ShipmentReversal)
            {
                kept.Add(change);
                continue;
            }

            var numbers = change.SerialNumbers.Where(x =>
                set.SerialNumbers.GetValueOrDefault(UnitNumbers.Key(x)) is
                    { StatusId: SerialNumberStatus.Shipped } serialNumber &&
                serialNumber.ShipmentId == change.Source.ObjectId).ToList();
            var quantity = change.Quantity - (change.SerialNumbers.Count - numbers.Count);
            if (quantity > 0)
                kept.Add(change with { Quantity = quantity, SerialNumbers = numbers });
        }

        return kept;
    }

    /// <summary>
    ///     The order a shipment item belongs to, as its shipped serial numbers record it
    /// </summary>
    public async Task<ShipmentUnitInfo> ShipmentInfo(string shipmentId, string shipmentItemId)
    {
        var shipment = string.IsNullOrEmpty(shipmentId) ? null : await shipmentRepository.GetByIdAsync(shipmentId);
        var item = shipment?.ShipmentItems.FirstOrDefault(x => x.Id == shipmentItemId);
        return new ShipmentUnitInfo(shipmentId, shipment?.OrderId, item?.OrderItemId,
            shipment?.CreatedOnUtc ?? DateTime.UtcNow, item?.WarrantyUntilUtc);
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

        await CountInStock(set, product, bucket.CombinationId, bucket.WarehouseId);
        set.InitialPool.TryAdd(bucket.Key, bucket.Stock - Assigned(set, bucket));

        return set.Mode switch {
            InventoryTrackingMode.Batch => await ApplyBatch(set, product, bucket, delta, change),
            InventoryTrackingMode.SerialNumber => await ApplySerialNumbers(set, product, bucket, delta, change),
            _ => null
        };
    }

    /// <summary>
    ///     Applies again the units of an entry whose writes may have been lost: a record stamped with the entry's
    ///     revision or a later one has it already and is left alone. Nothing is checked: the entry was checked when it
    ///     was written
    /// </summary>
    public async Task Replay(UnitSet set, Product product, UnitBucket bucket, InventoryJournal entry)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(bucket);
        ArgumentNullException.ThrowIfNull(entry);

        var change = new StockChange(bucket.Attributes, bucket.WarehouseId, entry.InQty - entry.OutQty, false,
            entry.MovementTypeId, new StockSource(entry.ObjectType, entry.ObjectId, entry.PositionId)) {
            BatchNumber = entry.BatchNumber, UnitCost = entry.UnitCost
        };

        if (set.Mode == InventoryTrackingMode.Batch && !string.IsNullOrWhiteSpace(entry.BatchNumber))
        {
            var key = UnitNumbers.Key(entry.BatchNumber);
            var batch = Find(set, bucket, key);
            if (batch != null && batch.StockRevision >= entry.StockRevision)
                return;

            if (batch == null)
            {
                batch = (await Receive(set, product, bucket, key, change)).Batch;
                batch.ExpiryDateUtc ??= Date(entry.ExpiryDateUtc);
            }

            batch.Quantity += entry.MovementTypeId == InventoryMovementType.TrackingAssignment
                ? entry.InQty
                : entry.InQty - entry.OutQty;
            batch.StockRevision = entry.StockRevision;
            set.Changed.Add(batch);
            return;
        }

        if (set.Mode != InventoryTrackingMode.SerialNumber || entry.SerialNumbers is not { Count: > 0 } ||
            entry.MovementTypeId == InventoryMovementType.TransferOut)
            //a transfer's entry in the other warehouse places its numbers
            return;

        await CountInStock(set, product, bucket.CombinationId, bucket.WarehouseId);
        var shipment = entry.MovementTypeId == InventoryMovementType.Shipment
            ? await ShipmentInfo(entry.ObjectId, entry.PositionId)
            : null;
        foreach (var number in entry.SerialNumbers)
        {
            var serialNumber = set.SerialNumbers.GetValueOrDefault(UnitNumbers.Key(number));
            if (serialNumber != null && serialNumber.StockRevision >= entry.StockRevision)
                continue;

            if (serialNumber == null)
                serialNumber = await Create(set, product, bucket, number, change);
            else
                await Leave(set, product, serialNumber);

            switch (entry.MovementTypeId)
            {
                case InventoryMovementType.Shipment:
                    serialNumber.StatusId = SerialNumberStatus.Shipped;
                    serialNumber.WarehouseId = bucket.WarehouseId ?? "";
                    serialNumber.OrderId = shipment.OrderId;
                    serialNumber.OrderItemId = shipment.OrderItemId;
                    serialNumber.ShipmentId = shipment.ShipmentId;
                    serialNumber.ShippedOnUtc = shipment.ShippedOnUtc;
                    serialNumber.WarrantyUntilUtc = shipment.WarrantyUntilUtc;
                    break;
                case InventoryMovementType.ReturnScrapped:
                    serialNumber.StatusId = SerialNumberStatus.Scrapped;
                    break;
                case InventoryMovementType.StockCount when entry.OutQty > entry.InQty:
                    serialNumber.StatusId = SerialNumberStatus.Missing;
                    break;
                case not InventoryMovementType.TrackingAssignment when entry.OutQty > entry.InQty:
                    serialNumber.StatusId = SerialNumberStatus.Scrapped;
                    break;
                default:
                    await Place(set, serialNumber, bucket);
                    if (entry.MovementTypeId is InventoryMovementType.ShipmentReversal
                        or InventoryMovementType.ReturnReceipt)
                    {
                        serialNumber.OrderId = serialNumber.OrderItemId = serialNumber.ShipmentId = null;
                        serialNumber.ShippedOnUtc = serialNumber.WarrantyUntilUtc = null;
                    }

                    break;
            }

            serialNumber.StockRevision = entry.StockRevision;
            set.Changed.Add(serialNumber);
        }
    }

    /// <summary>
    ///     The changes a stock count of a serial-tracked bucket books: the serial numbers found are all the units the
    ///     bucket holds. A number in stock here is confirmed; one unknown, scrapped or missing is assigned to the stock
    ///     without numbers while it lasts, and beyond it booked in as found; a number in stock here and not found is
    ///     missing, and the stock without numbers left is booked out
    /// </summary>
    /// <returns>The changes, or the key of the error</returns>
    public async Task<(IReadOnlyList<StockChange> Changes, string Error)> ExpandCount(UnitSet set, Product product,
        UnitBucket bucket, StockChange change)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(bucket);
        ArgumentNullException.ThrowIfNull(change);

        var scanned = change.CountedSerialNumbers ?? [];
        if (UnitNumbers.Duplicates(scanned).Count > 0)
            return ([], UnitErrors.SerialDuplicate);

        var combinationId = bucket.CombinationId ?? "";
        var warehouseId = bucket.WarehouseId ?? "";
        foreach (var serialNumber in await serialNumberRepository.ToListAsync(serialNumberRepository.Table.Where(x =>
                     x.ProductId == product.Id && x.CombinationId == combinationId && x.WarehouseId == warehouseId &&
                     x.StatusId == SerialNumberStatus.InStock)))
            set.SerialNumbers.TryAdd(serialNumber.NumberKey, serialNumber);
        await CountInStock(set, product, combinationId, warehouseId);

        var pool = bucket.Stock - set.InStock[bucket.Key];
        var keys = scanned.Select(UnitNumbers.Key).ToHashSet();
        var assigned = new List<string>();
        var found = new List<string>();
        foreach (var number in scanned)
        {
            var serialNumber = set.SerialNumbers.GetValueOrDefault(UnitNumbers.Key(number));
            if (serialNumber is { StatusId: SerialNumberStatus.InStock } &&
                InBucket(serialNumber.CombinationId, serialNumber.WarehouseId, bucket))
                continue;
            if (serialNumber is { StatusId: SerialNumberStatus.InStock })
                return ([], UnitErrors.SerialInOtherWarehouse);
            if (serialNumber is { StatusId: SerialNumberStatus.Shipped })
                return ([], UnitErrors.SerialShipped);

            (assigned.Count < pool ? assigned : found).Add(number);
        }

        var missing = set.SerialNumbers.Values
            .Where(x => x.StatusId == SerialNumberStatus.InStock &&
                        InBucket(x.CombinationId, x.WarehouseId, bucket) && !keys.Contains(x.NumberKey))
            .Select(x => x.Number).ToList();
        var poolLeft = Math.Max(0, pool - assigned.Count);

        StockChange Part(int quantity, InventoryMovementType movementType, IReadOnlyList<string> numbers)
        {
            return change with {
                Quantity = quantity, SetTo = false, MovementType = movementType, SerialNumbers = numbers,
                CountedSerialNumbers = null
            };
        }

        var changes = new List<StockChange>();
        if (assigned.Count > 0)
            changes.Add(Part(assigned.Count, InventoryMovementType.TrackingAssignment, assigned));
        if (found.Count > 0)
            changes.Add(Part(found.Count, InventoryMovementType.StockCount, found));
        if (missing.Count + poolLeft > 0)
            changes.Add(Part(-(missing.Count + poolLeft), InventoryMovementType.StockCount, missing));
        return (changes, null);
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

        //a transfer puts in the other warehouse every serial number it takes out
        if (set.InTransit.Count > 0)
            return Task.FromResult(UnitErrors.SerialNotInStock);

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
    /// <summary>
    ///     Names the entry's batch and serial numbers as their records spell them, whatever spelling the change used:
    ///     the history of a unit finds its entries by the stored number
    /// </summary>
    public void Name(UnitSet set, StockChange change, InventoryJournal entry)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(entry);

        if (!set.Touched.TryGetValue(change, out var records))
            return;

        if (entry.BatchNumber != null &&
            records.OfType<ProductBatch>().FirstOrDefault(x => x.NumberKey == UnitNumbers.Key(entry.BatchNumber)) is
                { } batch)
            entry.BatchNumber = batch.Number;
        var serialNumbers = records.OfType<ProductSerialNumber>().ToList();
        entry.SerialNumbers = entry.SerialNumbers.Select(x =>
            serialNumbers.FirstOrDefault(s => s.NumberKey == UnitNumbers.Key(x))?.Number ?? x).ToList();
    }

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
                    //only what the ledger changes: the expiry task and a warehouse moved to another store write
                    //the batch's other fields without the product's lock
                    await batchRepository.UpdateOneAsync(x => x.Id == batch.Id,
                        UpdateBuilder<ProductBatch>.Create()
                            .Set(x => x.Quantity, batch.Quantity)
                            .Set(x => x.StockRevision, batch.StockRevision));
                    break;
                case ProductSerialNumber serialNumber when set.Added.Contains(serialNumber):
                    await serialNumberRepository.InsertAsync(serialNumber);
                    break;
                case ProductSerialNumber serialNumber:
                    //only what the ledger changes: the location, the device fields and the comments are written by
                    //the back office without the product's lock
                    await serialNumberRepository.UpdateOneAsync(x => x.Id == serialNumber.Id,
                        UpdateBuilder<ProductSerialNumber>.Create()
                            .Set(x => x.StatusId, serialNumber.StatusId)
                            .Set(x => x.CombinationId, serialNumber.CombinationId)
                            .Set(x => x.Attributes, serialNumber.Attributes)
                            .Set(x => x.WarehouseId, serialNumber.WarehouseId)
                            .Set(x => x.StoreId, serialNumber.StoreId)
                            .Set(x => x.UnitCost, serialNumber.UnitCost)
                            .Set(x => x.ReceivedOnUtc, serialNumber.ReceivedOnUtc)
                            .Set(x => x.ReceiptDocumentId, serialNumber.ReceiptDocumentId)
                            .Set(x => x.OrderId, serialNumber.OrderId)
                            .Set(x => x.OrderItemId, serialNumber.OrderItemId)
                            .Set(x => x.ShipmentId, serialNumber.ShipmentId)
                            .Set(x => x.ShippedOnUtc, serialNumber.ShippedOnUtc)
                            .Set(x => x.WarrantyUntilUtc, serialNumber.WarrantyUntilUtc)
                            .Set(x => x.StockRevision, serialNumber.StockRevision));
                    break;
            }
    }

    /// <summary>
    ///     Reads how many serial numbers are in stock in the bucket, once per call; the call keeps the count since
    /// </summary>
    private async Task CountInStock(UnitSet set, Product product, string combinationId, string warehouseId)
    {
        var key = (combinationId ?? "", warehouseId ?? "");
        if (set.Mode != InventoryTrackingMode.SerialNumber || set.InStock.ContainsKey(key))
            return;

        set.InStock[key] = await serialNumberRepository.CountAsync(serialNumberRepository.Table.Where(x =>
            x.ProductId == product.Id && x.CombinationId == key.Item1 && x.WarehouseId == key.Item2 &&
            x.StatusId == SerialNumberStatus.InStock));
    }

    /// <summary>
    ///     Takes a serial number in stock out of the count of its bucket
    /// </summary>
    private async Task Leave(UnitSet set, Product product, ProductSerialNumber serialNumber)
    {
        if (serialNumber.StatusId != SerialNumberStatus.InStock)
            return;

        await CountInStock(set, product, serialNumber.CombinationId, serialNumber.WarehouseId);
        set.InStock[(serialNumber.CombinationId ?? "", serialNumber.WarehouseId ?? "")]--;
    }

    private static int Assigned(UnitSet set, UnitBucket bucket)
    {
        return set.Mode switch {
            InventoryTrackingMode.Batch => set.Batches.Where(x => InBucket(x.CombinationId, x.WarehouseId, bucket))
                .Sum(x => x.Quantity),
            InventoryTrackingMode.SerialNumber => set.InStock.GetValueOrDefault(bucket.Key),
            _ => 0
        };
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

    private async Task<string> ApplySerialNumbers(UnitSet set, Product product, UnitBucket bucket, int delta,
        StockChange change)
    {
        var numbers = change.SerialNumbers;
        if (UnitNumbers.Duplicates(numbers).Count > 0)
            return UnitErrors.SerialDuplicate;

        switch (change.MovementType)
        {
            case InventoryMovementType.TrackingAssignment:
                if (numbers.Count != change.Quantity)
                    return UnitErrors.SerialCountMismatch;
                foreach (var number in numbers)
                {
                    var (serialNumber, error) = await Arrive(set, product, bucket, number, change);
                    if (error != null)
                        return error;
                    Touch(set, change, serialNumber);
                }

                return null;
            case InventoryMovementType.ReturnScrapped:
                foreach (var number in numbers)
                {
                    var serialNumber = set.SerialNumbers.GetValueOrDefault(UnitNumbers.Key(number));
                    if (serialNumber is not { StatusId: SerialNumberStatus.Shipped })
                        return UnitErrors.SerialNotShipped;
                    serialNumber.StatusId = SerialNumberStatus.Scrapped;
                    Touch(set, change, serialNumber);
                }

                return null;
        }

        if (delta > 0)
            return numbers.Count > delta ? UnitErrors.SerialCountMismatch : await In(set, product, bucket, change);
        if (delta < 0)
        {
            //a unit with a serial number is never shipped without it
            if (numbers.Count > -delta ||
                (change.MovementType == InventoryMovementType.Shipment && numbers.Count != -delta))
                return UnitErrors.SerialCountMismatch;
            return await Out(set, product, bucket, change);
        }

        return null;
    }

    private async Task<string> In(UnitSet set, Product product, UnitBucket bucket, StockChange change)
    {
        foreach (var number in change.SerialNumbers)
        {
            var key = UnitNumbers.Key(number);
            var serialNumber = set.SerialNumbers.GetValueOrDefault(key);
            string error = null;
            switch (change.MovementType)
            {
                case InventoryMovementType.TransferIn when set.InTransit.Remove(key):
                    await Place(set, serialNumber, bucket);
                    break;
                case InventoryMovementType.ShipmentReversal or InventoryMovementType.ReturnReceipt:
                    if (serialNumber is not { StatusId: SerialNumberStatus.Shipped } ||
                        (change.MovementType == InventoryMovementType.ShipmentReversal &&
                         serialNumber.ShipmentId != change.Source.ObjectId))
                        return UnitErrors.SerialNotShipped;
                    await Place(set, serialNumber, bucket);
                    serialNumber.OrderId = serialNumber.OrderItemId = serialNumber.ShipmentId = null;
                    serialNumber.ShippedOnUtc = serialNumber.WarrantyUntilUtc = null;
                    break;
                default:
                    (serialNumber, error) = await Arrive(set, product, bucket, number, change);
                    break;
            }

            if (error != null)
                return error;
            Touch(set, change, serialNumber);
        }

        return null;
    }

    private async Task<string> Out(UnitSet set, Product product, UnitBucket bucket, StockChange change)
    {
        var shipment = change.MovementType == InventoryMovementType.Shipment
            ? await ShipmentInfo(change.Source.ObjectId, change.Source.PositionId)
            : null;
        foreach (var number in change.SerialNumbers)
        {
            var key = UnitNumbers.Key(number);
            var serialNumber = set.SerialNumbers.GetValueOrDefault(key);
            var here = serialNumber is { StatusId: SerialNumberStatus.InStock } &&
                       InBucket(serialNumber.CombinationId, serialNumber.WarehouseId, bucket) &&
                       !set.InTransit.Contains(key);
            if (!here)
            {
                if (serialNumber is { StatusId: SerialNumberStatus.InStock })
                    return UnitErrors.SerialInOtherWarehouse;
                if (serialNumber?.StatusId == SerialNumberStatus.Shipped)
                    return UnitErrors.SerialShipped;
                if (shipment == null)
                    return UnitErrors.SerialNotInStock;
                //a shipment may name a number new for the product: it takes a unit of the stock without numbers
                serialNumber ??= await Create(set, product, bucket, number, change);
                serialNumber.CombinationId = bucket.CombinationId ?? "";
                serialNumber.Attributes = bucket.Attributes ?? [];
                serialNumber.WarehouseId = bucket.WarehouseId ?? "";
            }
            else
            {
                set.InStock[bucket.Key]--;
            }

            switch (change.MovementType)
            {
                case InventoryMovementType.Shipment:
                    serialNumber.StatusId = SerialNumberStatus.Shipped;
                    serialNumber.OrderId = shipment.OrderId;
                    serialNumber.OrderItemId = shipment.OrderItemId;
                    serialNumber.ShipmentId = shipment.ShipmentId;
                    serialNumber.ShippedOnUtc = shipment.ShippedOnUtc;
                    serialNumber.WarrantyUntilUtc = shipment.WarrantyUntilUtc;
                    break;
                case InventoryMovementType.TransferOut:
                    set.InTransit.Add(key);
                    break;
                case InventoryMovementType.StockCount:
                    serialNumber.StatusId = SerialNumberStatus.Missing;
                    break;
                default:
                    serialNumber.StatusId = SerialNumberStatus.Scrapped;
                    break;
            }

            Touch(set, change, serialNumber);
        }

        return null;
    }

    /// <summary>
    ///     A serial number taking stock in the bucket: new for the product, or one scrapped or missing coming back
    /// </summary>
    private async Task<(ProductSerialNumber SerialNumber, string Error)> Arrive(UnitSet set, Product product,
        UnitBucket bucket, string number, StockChange change)
    {
        var serialNumber = set.SerialNumbers.GetValueOrDefault(UnitNumbers.Key(number));
        switch (serialNumber?.StatusId)
        {
            case SerialNumberStatus.InStock:
                return (null, UnitErrors.SerialInStock);
            case SerialNumberStatus.Shipped:
                return (null, UnitErrors.SerialShipped);
            case null:
                serialNumber = await Create(set, product, bucket, number, change);
                break;
        }

        await Place(set, serialNumber, bucket);
        if (change.MovementType == InventoryMovementType.Receipt)
        {
            serialNumber.UnitCost = change.UnitCost;
            serialNumber.ReceivedOnUtc = DateTime.UtcNow;
            serialNumber.ReceiptDocumentId = ReceiptDocument(change);
        }

        return (serialNumber, null);
    }

    private async Task<ProductSerialNumber> Create(UnitSet set, Product product, UnitBucket bucket, string number,
        StockChange change)
    {
        var serialNumber = new ProductSerialNumber {
            ProductId = product.Id,
            Number = number.Trim(),
            NumberKey = UnitNumbers.Key(number),
            CombinationId = bucket.CombinationId ?? "",
            Attributes = bucket.Attributes ?? [],
            WarehouseId = bucket.WarehouseId ?? "",
            StoreId = await StoreOf(bucket.WarehouseId),
            UnitCost = change.UnitCost,
            ReceivedOnUtc = DateTime.UtcNow,
            ReceiptDocumentId = ReceiptDocument(change)
        };
        set.SerialNumbers[serialNumber.NumberKey] = serialNumber;
        set.Added.Add(serialNumber);
        return serialNumber;
    }

    /// <summary>
    ///     Puts the serial number in stock in the bucket
    /// </summary>
    private async Task Place(UnitSet set, ProductSerialNumber serialNumber, UnitBucket bucket)
    {
        if ((serialNumber.WarehouseId ?? "") != (bucket.WarehouseId ?? "") || serialNumber.StoreId == null)
            serialNumber.StoreId = await StoreOf(bucket.WarehouseId);
        serialNumber.StatusId = SerialNumberStatus.InStock;
        serialNumber.CombinationId = bucket.CombinationId ?? "";
        serialNumber.Attributes = bucket.Attributes ?? [];
        serialNumber.WarehouseId = bucket.WarehouseId ?? "";
        set.InStock[bucket.Key] = set.InStock.GetValueOrDefault(bucket.Key) + 1;
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
            ReceiptDocumentId = known != null ? known.ReceiptDocumentId : ReceiptDocument(change),
            //what is known of the batch goes with it; a location is a place in the other warehouse
            ManufacturedOnUtc = known?.ManufacturedOnUtc,
            Supplier = known?.Supplier,
            SupplierBatchNumber = known?.SupplierBatchNumber,
            CertificateNumber = known?.CertificateNumber,
            AdminComment = known?.AdminComment,
            CustomerComment = known?.CustomerComment
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

/// <summary>
///     The shipment item a shipped serial number belongs to
/// </summary>
public sealed record ShipmentUnitInfo(
    string ShipmentId,
    string OrderId,
    string OrderItemId,
    DateTime ShippedOnUtc,
    DateTime? WarrantyUntilUtc);
