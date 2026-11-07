using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Inventory;

/// <summary>
///     Applies stock changes to a product read under its <see cref="ProductStockLock" /> key and writes them to the
///     inventory journal. The caller holds the key, and the lock is not re-entrant: code holding it calls this, never
///     IStockMovementService.
///     The entries are written before the product, and the product write stamps the revision of its last entry. An entry
///     newer than the product's revision is one whose product write was lost; it is applied before the next change, so
///     the stored stock always comes back to the sum of the journal.
/// </summary>
public sealed class StockLedger(
    IRepository<Product> productRepository,
    IRepository<InventoryJournal> inventoryJournalRepository,
    IWarehouseService warehouseService,
    StockSideEffects stockSideEffects,
    IMediator mediator,
    StockUnits stockUnits)
{
    /// <summary>
    ///     Applies the changes to the product, which the caller read under the product's lock
    /// </summary>
    /// <returns>The keys of the errors; nothing is written when there is one</returns>
    public Task<IList<string>> Apply(Product product, IReadOnlyList<StockChange> changes)
    {
        return Run(product, changes, true);
    }

    /// <summary>
    ///     Checks the changes by the rules <see cref="Apply" /> books them with, against the stock and units stored now,
    ///     and writes nothing; the product passed is changed in memory
    /// </summary>
    /// <returns>The keys of the errors</returns>
    public Task<IList<string>> Check(Product product, IReadOnlyList<StockChange> changes)
    {
        return Run(product, changes, false);
    }

    private async Task<IList<string>> Run(Product product, IReadOnlyList<StockChange> changes, bool write)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(changes);

        if (!Tracked(product.ManageInventoryMethodId))
            return [StockErrors.StockNotTracked];

        //a product tracking batches or serial numbers moves them with its stock
        var (repaired, units) = await RepairWithUnits(product, changes);
        Sum(product);
        var before = Available(product);
        if (units != null)
            changes = stockUnits.StillShipped(units, changes);

        //a counted serial line is worked out against the units stored now
        if (units != null && changes.Any(x => x.CountedSerialNumbers != null))
        {
            var expanded = new List<StockChange>();
            foreach (var change in changes)
            {
                if (change.CountedSerialNumbers == null)
                {
                    expanded.Add(change);
                    continue;
                }

                var bucket = Locate(product, null, change.Attributes, change.WarehouseId, out var locateError);
                var (counted, countError) = bucket == null
                    ? ([], locateError)
                    : await stockUnits.ExpandCount(units, product, UnitsOf(bucket), change);
                if (countError != null)
                    return [countError];
                expanded.AddRange(counted);
            }

            changes = expanded;
        }

        var errors = new List<string>();
        var entries = new List<InventoryJournal>();
        var unitEntries = new List<(StockChange Change, InventoryJournal Entry)>();
        var unitBuckets = new Dictionary<(string, string), Bucket>();
        var movementTypes = new Dictionary<(string, string), InventoryMovementType>();
        var reservedChanged = false;
        var costChanged = false;
        //the product's physical stock before each change, for the average cost of a receipt
        var onHand = product.StockQuantity;
        foreach (var change in changes)
        {
            var bucket = Locate(product, null, change.Attributes, change.WarehouseId, out var error);
            if (bucket == null)
            {
                errors.Add(error);
                continue;
            }

            //an assignment gives units to stock without them, and a scrapped return was out of stock already:
            //neither moves stock
            var unitsOnly = change.MovementType is InventoryMovementType.TrackingAssignment
                or InventoryMovementType.ReturnScrapped;
            var delta = unitsOnly ? 0 : change.SetTo ? change.Quantity - bucket.Stock : change.Quantity;
            if (change.MovementType == InventoryMovementType.TransferOut &&
                product.BackorderModeId == BackorderMode.NoBackorders && bucket.Stock - bucket.Reserved < -delta)
            {
                errors.Add(StockErrors.NotEnoughAvailable);
                continue;
            }

            if (units != null)
            {
                var unitError = await stockUnits.Apply(units, product, UnitsOf(bucket), delta, change);
                if (unitError != null)
                {
                    errors.Add(unitError);
                    continue;
                }

                unitBuckets[bucket.Key] = bucket;
            }
            else if (unitsOnly)
            {
                errors.Add(StockErrors.StockNotTracked);
                continue;
            }

            if (unitsOnly)
            {
                var entry = Entry(product, bucket, 0, change.MovementType, change.Source, change);
                if (change.MovementType == InventoryMovementType.TrackingAssignment)
                    entry.InQty = entry.OutQty = change.Quantity;
                entries.Add(entry);
                unitEntries.Add((change, entry));
            }
            else if (delta != 0)
            {
                if (change is { MovementType: InventoryMovementType.Receipt, UnitCost: { } unitCost } && delta > 0)
                {
                    product.ProductCost = AverageCost(onHand, product.ProductCost, delta, unitCost);
                    costChanged = true;
                }

                onHand += delta;
                Move(bucket, delta, change.MovementType);
                var entry = Entry(product, bucket, delta, change.MovementType, change.Source,
                    units != null ? change : null);
                entries.Add(entry);
                unitEntries.Add((change, entry));
            }

            if (change.ReservedQuantity is { } reserved && reserved != bucket.Reserved)
            {
                bucket.Reserved = reserved;
                reservedChanged = true;
            }

            movementTypes[bucket.Key] = change.MovementType;
        }

        if (units != null && errors.Count == 0 &&
            await stockUnits.CheckPools(units, unitBuckets.Values.Select(UnitsOf)) is { } poolError)
            errors.Add(poolError);

        if (!write || errors.Count > 0 || (entries.Count == 0 && !reservedChanged && !repaired))
            return errors;

        if (units != null)
            foreach (var (change, entry) in unitEntries)
                stockUnits.Name(units, change, entry);
        await Insert(product, entries);
        if (units != null)
        {
            foreach (var (change, entry) in unitEntries)
                stockUnits.Stamp(units, change, entry);
            await stockUnits.Write(units);
        }

        await Write(product, costChanged);

        if (changes.Count > 0)
            await stockSideEffects.Run(product, Changed(product, before, Available(product), movementTypes,
                changes[^1].MovementType));

        return errors;
    }

    /// <summary>
    ///     Moves the product's history from its previous inventory method, warehouse mode or warehouse to the current
    ///     one: every bucket of the previous configuration is closed by an entry taking its stock out, and every bucket
    ///     of the current one starts again with its stored stock
    /// </summary>
    public async Task Reopen(Product product, ManageInventoryMethod previousMethod, bool previousMultipleWarehouses,
        string previousWarehouseId, string customerId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!Tracked(previousMethod) && !Tracked(product.ManageInventoryMethodId))
            return;

        var (_, units) = await RepairWithUnits(product, []);

        var source = new StockSource(nameof(Product), product.Id, CustomerId: customerId ?? "");
        //read before Sum, which works the sums out for the current configuration
        var entries = Buckets(product, previousMethod, previousMultipleWarehouses, previousWarehouseId)
            .Where(bucket => bucket.Stock != 0)
            .Select(bucket => Entry(product, bucket, -bucket.Stock, InventoryMovementType.ClosingBalance, source))
            .ToList();
        Sum(product);
        entries.AddRange(Buckets(product, product.ManageInventoryMethodId, product.UseMultipleWarehouses,
                product.WarehouseId)
            .Select(bucket => Entry(product, bucket, bucket.Stock, InventoryMovementType.OpeningBalance, source)));

        await Insert(product, entries);
        if (units != null)
            await stockUnits.Write(units);
        await Write(product);
    }

    /// <summary>
    ///     Marks the stock each bucket holds when batch or serial tracking is switched on: it becomes stock without
    ///     units, assigned later by a document, a shipment or a stock count. The entries move nothing
    /// </summary>
    public async Task StartTracking(Product product, string customerId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!Tracked(product.ManageInventoryMethodId))
            return;

        var (_, units) = await RepairWithUnits(product, []);
        Sum(product);

        var source = new StockSource(nameof(Product), product.Id, CustomerId: customerId ?? "");
        var entries = Buckets(product, product.ManageInventoryMethodId, product.UseMultipleWarehouses,
                product.WarehouseId)
            .Where(bucket => bucket.Stock != 0)
            .Select(bucket => Entry(product, bucket, 0, InventoryMovementType.TrackingStarted,
                source with { Comment = $"pool {bucket.Stock}" }))
            .ToList();

        await Insert(product, entries);
        if (units != null)
            await stockUnits.Write(units);
        await Write(product);
    }

    /// <summary>
    ///     Works the product and combination sums out again from their buckets and writes them
    /// </summary>
    public async Task RecomputeSums(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var (_, units) = await RepairWithUnits(product, []);
        if (units != null)
            await stockUnits.Write(units);
        await Write(product);
    }

    /// <summary>
    ///     The weighted average cost after a receipt: what is on hand at the current cost and what is received at its
    ///     price; stock below zero holds no value. Kept at product level, also for a product stocked by attributes
    /// </summary>
    private static double AverageCost(int onHand, double cost, int quantity, decimal unitCost)
    {
        var held = Math.Max(0, onHand);
        return (double)Math.Round((held * (decimal)cost + quantity * unitCost) / (held + quantity), 4,
            MidpointRounding.AwayFromZero);
    }

    private static bool Tracked(ManageInventoryMethod method)
    {
        return method is ManageInventoryMethod.ManageStock or ManageInventoryMethod.ManageStockByAttributes;
    }

    /// <summary>
    ///     Applies the entries whose product write was lost, with their units, to the product and the units read
    /// </summary>
    /// <returns>Whether an entry was applied, and the units read for a product tracking them, or null</returns>
    private async Task<(bool Repaired, UnitSet Units)> RepairWithUnits(Product product,
        IReadOnlyList<StockChange> changes)
    {
        var pending = await Pending(product);
        var units = InventoryTracking.Tracked(product) ? await stockUnits.Load(product, changes, pending) : null;
        if (units != null)
            foreach (var entry in pending)
                if (Locate(product, entry.CombinationId, entry.Attributes, entry.WarehouseId, out _) is { } bucket)
                    await stockUnits.Replay(units, product, UnitsOf(bucket), entry);

        return (Repair(product, pending), units);
    }

    /// <summary>
    ///     The entries whose product write was lost, oldest first
    /// </summary>
    private async Task<IList<InventoryJournal>> Pending(Product product)
    {
        return await inventoryJournalRepository.ToListAsync(inventoryJournalRepository.Table
            .Where(x => x.ProductId == product.Id && x.StockRevision > product.StockRevision)
            .OrderBy(x => x.StockRevision));
    }

    /// <summary>
    ///     Applies the entries whose product write was lost; the balances of a reopening record the stored stock and
    ///     change nothing
    /// </summary>
    private static bool Repair(Product product, IList<InventoryJournal> pending)
    {
        foreach (var entry in pending)
        {
            if (entry.MovementTypeId is not (InventoryMovementType.OpeningBalance or
                    InventoryMovementType.ClosingBalance or InventoryMovementType.Legacy) &&
                Locate(product, entry.CombinationId, entry.Attributes, entry.WarehouseId, out _) is { } bucket)
                Move(bucket, entry.InQty - entry.OutQty, entry.MovementTypeId);

            product.StockRevision = entry.StockRevision;
        }

        return pending.Count > 0;
    }

    /// <summary>
    ///     Changes the bucket's stock; a shipment takes its quantity out of the reservation too, and its reversal puts it
    ///     back, as booking a shipment always did
    /// </summary>
    private static void Move(Bucket bucket, int delta, InventoryMovementType movementType)
    {
        bucket.Stock += delta;
        bucket.Reserved = movementType switch {
            InventoryMovementType.Shipment => Math.Max(0, bucket.Reserved + delta),
            InventoryMovementType.ShipmentReversal => bucket.Reserved + delta,
            _ => bucket.Reserved
        };
    }

    /// <summary>
    ///     The bucket a change applies to, by the product's inventory method; a missing warehouse row is created
    /// </summary>
    private static Bucket Locate(Product product, string combinationId, IList<CustomAttribute> attributes,
        string warehouseId, out string error)
    {
        error = null;
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
        {
            //a product kept in one warehouse has all its stock there, whatever warehouse the change names
            if (!product.UseMultipleWarehouses)
                return new Bucket(product, null, "", product.WarehouseId ?? "");

            if (string.IsNullOrEmpty(warehouseId))
            {
                error = StockErrors.WarehouseRequired;
                return null;
            }

            var row = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (row == null)
            {
                row = new ProductWarehouseInventory { WarehouseId = warehouseId };
                product.ProductWarehouseInventory.Add(row);
            }

            return new Bucket(row, null, warehouseId, warehouseId);
        }

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
        {
            error = StockErrors.StockNotTracked;
            return null;
        }

        var combination = !string.IsNullOrEmpty(combinationId)
            ? product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == combinationId)
            : attributes is { Count: > 0 }
                ? product.FindProductAttributeCombination(attributes)
                : null;
        if (combination == null)
        {
            error = StockErrors.CombinationNotFound;
            return null;
        }

        if (!product.UseMultipleWarehouses)
            return new Bucket(combination, combination, "", product.WarehouseId ?? "");

        if (string.IsNullOrEmpty(warehouseId))
        {
            error = StockErrors.WarehouseRequired;
            return null;
        }

        var combinationRow = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
        if (combinationRow == null)
        {
            combinationRow = new ProductCombinationWarehouseInventory { WarehouseId = warehouseId };
            combination.WarehouseInventory.Add(combinationRow);
        }

        return new Bucket(combinationRow, combination, warehouseId, warehouseId);
    }

    /// <summary>
    ///     Every bucket the product has under the given inventory method, warehouse mode and warehouse
    /// </summary>
    private static IEnumerable<Bucket> Buckets(Product product, ManageInventoryMethod method, bool multipleWarehouses,
        string warehouseId)
    {
        switch (method)
        {
            case ManageInventoryMethod.ManageStock when !multipleWarehouses:
                yield return new Bucket(product, null, "", warehouseId ?? "");
                break;
            case ManageInventoryMethod.ManageStock:
                foreach (var row in product.ProductWarehouseInventory)
                    yield return new Bucket(row, null, row.WarehouseId, row.WarehouseId);
                break;
            case ManageInventoryMethod.ManageStockByAttributes:
                foreach (var combination in product.ProductAttributeCombinations)
                    if (!multipleWarehouses)
                        yield return new Bucket(combination, combination, "", warehouseId ?? "");
                    else
                        foreach (var row in combination.WarehouseInventory)
                            yield return new Bucket(row, combination, row.WarehouseId, row.WarehouseId);
                break;
        }
    }

    /// <summary>
    ///     With multiple warehouses a product's or combination's stock is the sum of its rows, and a product stocked by
    ///     attributes holds the sum of its combinations
    /// </summary>
    private static void Sum(Product product)
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
        {
            product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
            product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);
        }

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
            return;

        if (product.UseMultipleWarehouses)
            foreach (var combination in product.ProductAttributeCombinations)
            {
                combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);
            }

        product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
        product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
    }

    /// <param name="units">The change whose units the entry names; null for a product tracking none</param>
    private static InventoryJournal Entry(Product product, Bucket bucket, int delta,
        InventoryMovementType movementType, StockSource source, StockChange units = null)
    {
        return new InventoryJournal {
            BatchNumber = string.IsNullOrWhiteSpace(units?.BatchNumber) ? null : units.BatchNumber.Trim(),
            SerialNumbers = units?.SerialNumbers.Select(x => x.Trim()).ToList() ?? [],
            ExpiryDateUtc = units?.ExpiryDateUtc,
            UnitCost = units?.UnitCost,
            ProductId = product.Id,
            CombinationId = bucket.CombinationId,
            Attributes = bucket.Attributes,
            WarehouseId = bucket.WarehouseId,
            InQty = Math.Max(0, delta),
            OutQty = Math.Max(0, -delta),
            MovementTypeId = movementType,
            CreateDateUtc = DateTime.UtcNow,
            ObjectType = source.ObjectType,
            ObjectId = source.ObjectId,
            PositionId = source.PositionId,
            Reference = source.Reference,
            CustomerId = source.CustomerId,
            Comments = source.Comment
        };
    }

    private async Task Insert(Product product, IList<InventoryJournal> entries)
    {
        var warehouseStores = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            if (!warehouseStores.TryGetValue(entry.WarehouseId, out var storeId))
            {
                storeId = string.IsNullOrEmpty(entry.WarehouseId)
                    ? ""
                    : (await warehouseService.GetWarehouseById(entry.WarehouseId))?.StoreId ?? "";
                warehouseStores[entry.WarehouseId] = storeId;
            }

            entry.StoreId = storeId;
            entry.StockRevision = ++product.StockRevision;
            await inventoryJournalRepository.InsertAsync(entry);
        }
    }

    /// <summary>
    ///     Writes every stock field of the product in one update, so they never land out of step
    /// </summary>
    private async Task Write(Product product, bool costChanged = false)
    {
        Sum(product);
        if (product.ReservedQuantity < 0)
            product.ReservedQuantity = 0;

        product.LowStock = (product.MinStockQuantity > 0 &&
                            product.MinStockQuantity >= product.StockQuantity - product.ReservedQuantity) ||
                           product.StockQuantity - product.ReservedQuantity <= 0;

        var update = UpdateBuilder<Product>.Create()
            .Set(x => x.StockQuantity, product.StockQuantity)
            .Set(x => x.ReservedQuantity, product.ReservedQuantity)
            .Set(x => x.LowStock, product.LowStock)
            .Set(x => x.ProductWarehouseInventory, product.ProductWarehouseInventory)
            .Set(x => x.ProductAttributeCombinations, product.ProductAttributeCombinations)
            .Set(x => x.StockRevision, product.StockRevision);
        if (costChanged)
            update.Set(x => x.ProductCost, product.ProductCost);

        await productRepository.UpdateOneAsync(x => x.Id == product.Id, update);

        await mediator.Publish(new ProductChangedEvent(product.Id));
        await mediator.Publish(new UpdateStockEvent(product));
        await mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     Available stock by (combination, warehouse row), totals included under an empty warehouse
    /// </summary>
    private static Dictionary<(string, string), int> Available(Product product)
    {
        var available = new Dictionary<(string, string), int> {
            [("", "")] = product.StockQuantity - product.ReservedQuantity
        };
        foreach (var row in product.ProductWarehouseInventory)
            available[("", row.WarehouseId)] = row.StockQuantity - row.ReservedQuantity;
        foreach (var combination in product.ProductAttributeCombinations)
        {
            available[(combination.Id, "")] = combination.StockQuantity - combination.ReservedQuantity;
            foreach (var row in combination.WarehouseInventory)
                available[(combination.Id, row.WarehouseId)] = row.StockQuantity - row.ReservedQuantity;
        }

        return available;
    }

    private static List<BucketChange> Changed(Product product, Dictionary<(string, string), int> before,
        Dictionary<(string, string), int> after, Dictionary<(string, string), InventoryMovementType> movementTypes,
        InventoryMovementType totalMovementType)
    {
        var changed = new List<BucketChange>();
        foreach (var ((combinationId, warehouseId), availableAfter) in after)
        {
            var availableBefore = before.GetValueOrDefault((combinationId, warehouseId));
            if (availableBefore == availableAfter)
                continue;

            var attributes = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == combinationId)
                ?.Attributes ?? [];
            changed.Add(new BucketChange(combinationId, attributes, warehouseId, availableBefore, availableAfter,
                movementTypes.GetValueOrDefault((combinationId, warehouseId), totalMovementType)));
        }

        return changed;
    }

    private static UnitBucket UnitsOf(Bucket bucket)
    {
        return new UnitBucket(bucket.CombinationId, bucket.Attributes, bucket.WarehouseId, bucket.Stock);
    }

    /// <summary>
    ///     One stock bucket: the product, a combination, or a warehouse row of either
    /// </summary>
    /// <param name="target">the object holding the bucket's quantities</param>
    /// <param name="combination">the combination, for a product stocked by attributes</param>
    /// <param name="rowWarehouseId">the warehouse of a row; empty for a product or combination</param>
    /// <param name="warehouseId">the warehouse the journal entry names</param>
    private sealed class Bucket(object target, ProductAttributeCombination combination, string rowWarehouseId,
        string warehouseId)
    {
        public (string, string) Key => (CombinationId, rowWarehouseId);
        public string CombinationId => combination?.Id ?? "";
        public IList<CustomAttribute> Attributes => combination?.Attributes ?? new List<CustomAttribute>();
        public string WarehouseId => warehouseId;

        public int Stock {
            get => target switch {
                Product x => x.StockQuantity,
                ProductAttributeCombination x => x.StockQuantity,
                ProductWarehouseInventory x => x.StockQuantity,
                ProductCombinationWarehouseInventory x => x.StockQuantity,
                _ => throw new InvalidOperationException()
            };
            set {
                switch (target)
                {
                    case Product x: x.StockQuantity = value; break;
                    case ProductAttributeCombination x: x.StockQuantity = value; break;
                    case ProductWarehouseInventory x: x.StockQuantity = value; break;
                    case ProductCombinationWarehouseInventory x: x.StockQuantity = value; break;
                }
            }
        }

        public int Reserved {
            get => target switch {
                Product x => x.ReservedQuantity,
                ProductAttributeCombination x => x.ReservedQuantity,
                ProductWarehouseInventory x => x.ReservedQuantity,
                ProductCombinationWarehouseInventory x => x.ReservedQuantity,
                _ => throw new InvalidOperationException()
            };
            set {
                switch (target)
                {
                    case Product x: x.ReservedQuantity = value; break;
                    case ProductAttributeCombination x: x.ReservedQuantity = value; break;
                    case ProductWarehouseInventory x: x.ReservedQuantity = value; break;
                    case ProductCombinationWarehouseInventory x: x.ReservedQuantity = value; break;
                }
            }
        }
    }
}
