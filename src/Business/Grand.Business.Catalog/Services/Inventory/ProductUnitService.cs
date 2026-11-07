using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Catalog.Services.Inventory;

public class ProductUnitService(
    IRepository<ProductBatch> batchRepository,
    IRepository<ProductSerialNumber> serialNumberRepository,
    IRepository<Product> productRepository,
    ISettingService settingService,
    IRepository<InventoryJournal> journalRepository,
    IRepository<Shipment> shipmentRepository,
    IRepository<Order> orderRepository,
    IWarehouseService warehouseService,
    StockLedger stockLedger) : IProductUnitService
{
    /// <summary>
    ///     The furthest a product's own warning days reach: batches expiring later are not read
    /// </summary>
    private const int MaxWarningDays = 366;

    public async Task<IList<ExpiringBatch>> BatchesToNotify(DateTime today)
    {
        var date = DateTime.SpecifyKind(today.Date, DateTimeKind.Utc);
        var until = date.AddDays(MaxWarningDays);
        //a batch reported expired has nothing more to report
        var batches = await batchRepository.ToListAsync(batchRepository.Table.Where(x =>
            x.Quantity > 0 && x.ExpiryDateUtc != null && x.ExpiryDateUtc <= until &&
            x.ExpiryNotifiedStatusId != BatchExpiryStatus.Expired));
        if (batches.Count == 0)
            return [];

        var settings = await settingService.LoadSetting<InventorySettings>();
        var productIds = batches.Select(x => x.ProductId).Distinct().ToList();
        var products = (await productRepository.ToListAsync(productRepository.Table.Where(x =>
            productIds.Contains(x.Id)))).ToDictionary(x => x.Id);
        var warehouses = new Dictionary<string, string>();
        var expiring = new List<ExpiringBatch>();
        foreach (var batch in batches)
        {
            if (!products.TryGetValue(batch.ProductId, out var product))
                continue;

            var status = BatchExpiry.Status(batch.ExpiryDateUtc, date, BatchExpiry.WarningDays(product, settings));
            if (status is not (BatchExpiryStatus.Expiring or BatchExpiryStatus.Expired) ||
                status == batch.ExpiryNotifiedStatusId)
                continue;

            if (!warehouses.TryGetValue(batch.WarehouseId ?? "", out var warehouse))
                warehouses[batch.WarehouseId ?? ""] = warehouse = string.IsNullOrEmpty(batch.WarehouseId)
                    ? ""
                    : (await warehouseService.GetWarehouseById(batch.WarehouseId))?.Name ?? batch.WarehouseId;
            expiring.Add(new ExpiringBatch(batch, product.Name, product.Sku, warehouse, status.Value));
        }

        return expiring.OrderBy(x => x.Batch.ExpiryDateUtc).ToList();
    }

    public async Task MarkNotified(IEnumerable<ExpiringBatch> batches)
    {
        ArgumentNullException.ThrowIfNull(batches);

        //one field: the ledger owns the quantity
        foreach (var batch in batches)
        {
            batch.Batch.ExpiryNotifiedStatusId = batch.Status;
            await batchRepository.UpdateField(batch.Batch.Id, x => x.ExpiryNotifiedStatusId,
                (BatchExpiryStatus?)batch.Status);
        }
    }

    public async Task<(int Expiring, int Expired)> ExpiryCounts(string storeId, DateTime today)
    {
        var date = DateTime.SpecifyKind(today.Date, DateTimeKind.Utc);
        var warningEnd = date.AddDays((await settingService.LoadSetting<InventorySettings>()).ExpiryWarningDays);
        var query = batchRepository.Table.Where(x => x.Quantity > 0 && x.ExpiryDateUtc != null);
        if (storeId != null)
            query = query.Where(x => x.StoreId == storeId);

        return (await batchRepository.CountAsync(query.Where(x => x.ExpiryDateUtc >= date && x.ExpiryDateUtc <= warningEnd)),
            await batchRepository.CountAsync(query.Where(x => x.ExpiryDateUtc < date)));
    }

    public Task<IPagedList<ProductSerialNumber>> SearchSerialNumbers(UnitSearch search, int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(search);

        var query = serialNumberRepository.Table;
        var key = UnitNumbers.Key(search.Number);
        if (key != "")
            query = search.Prefix
                ? query.Where(x => x.NumberKey.StartsWith(key))
                : query.Where(x => x.NumberKey == key);
        if (!string.IsNullOrEmpty(search.ProductId))
            query = query.Where(x => x.ProductId == search.ProductId);
        if (search.Status.HasValue)
            query = query.Where(x => x.StatusId == search.Status.Value);
        if (!string.IsNullOrEmpty(search.WarehouseId))
            query = query.Where(x => x.WarehouseId == search.WarehouseId);
        if (search.StoreId != null)
            query = query.Where(x => x.StoreId == search.StoreId);

        return serialNumberRepository.PagedAsync(query.OrderBy(x => x.NumberKey), pageIndex, pageSize);
    }

    public async Task<IPagedList<ProductBatch>> SearchBatches(UnitSearch search, int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(search);

        var query = batchRepository.Table;
        var key = UnitNumbers.Key(search.Number);
        if (key != "")
            query = search.Prefix
                ? query.Where(x => x.NumberKey.StartsWith(key))
                : query.Where(x => x.NumberKey == key);
        if (!string.IsNullOrEmpty(search.ProductId))
            query = query.Where(x => x.ProductId == search.ProductId);
        if (!string.IsNullOrEmpty(search.WarehouseId))
            query = query.Where(x => x.WarehouseId == search.WarehouseId);
        if (search.StoreId != null)
            query = query.Where(x => x.StoreId == search.StoreId);
        if (search.WithStockOnly)
            query = query.Where(x => x.Quantity > 0);

        //by the inventory setting's warning days: a product's own days show on each batch
        var today = DateTime.SpecifyKind(search.Today.Date, DateTimeKind.Utc);
        var warningEnd = today.AddDays((await settingService.LoadSetting<InventorySettings>()).ExpiryWarningDays);
        query = search.ExpiryStatus switch {
            BatchExpiryStatus.Expired => query.Where(x => x.ExpiryDateUtc != null && x.ExpiryDateUtc < today),
            BatchExpiryStatus.Expiring => query.Where(x =>
                x.ExpiryDateUtc != null && x.ExpiryDateUtc >= today && x.ExpiryDateUtc <= warningEnd),
            BatchExpiryStatus.Valid => query.Where(x => x.ExpiryDateUtc != null && x.ExpiryDateUtc > warningEnd),
            _ => query
        };

        return await batchRepository.PagedAsync(query.OrderBy(x => x.ExpiryDateUtc).ThenBy(x => x.NumberKey),
            pageIndex, pageSize);
    }

    public Task<ProductSerialNumber> GetSerialNumber(string id)
    {
        return serialNumberRepository.GetByIdAsync(id);
    }

    public Task<ProductBatch> GetBatch(string id)
    {
        return batchRepository.GetByIdAsync(id);
    }

    public Task<IPagedList<InventoryJournal>> History(ProductBatch batch, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(batch);

        return journalRepository.PagedAsync(BatchEntries(batch).OrderByDescending(x => x.StockRevision), pageIndex,
            pageSize);
    }

    public Task<IPagedList<InventoryJournal>> History(ProductSerialNumber serialNumber, IList<string> warehouseIds,
        int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(serialNumber);

        var query = journalRepository.Table.Where(x =>
            x.ProductId == serialNumber.ProductId && x.SerialNumbers.Contains(serialNumber.Number));
        if (warehouseIds != null)
            query = query.Where(x => warehouseIds.Contains(x.WarehouseId));
        return journalRepository.PagedAsync(query.OrderByDescending(x => x.StockRevision), pageIndex, pageSize);
    }

    public async Task<IPagedList<BatchRecipient>> BatchRecipients(ProductBatch batch, int pageIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(batch);

        //only deleting a shipment reverses it, and it gives back all it shipped: a reversed shipment kept nothing.
        //The reversed ones are few, the shipped ones are paged in the database
        var shipped = BatchEntries(batch).Where(x => x.ObjectType == nameof(Shipment));
        var reversed = await journalRepository.ToListAsync(shipped
            .Where(x => x.MovementTypeId == InventoryMovementType.ShipmentReversal).Select(x => x.ObjectId));
        var entries = await journalRepository.PagedAsync(shipped
            .Where(x => x.MovementTypeId == InventoryMovementType.Shipment && !reversed.Contains(x.ObjectId))
            .OrderBy(x => x.StockRevision), pageIndex, pageSize);

        //two reads for the page, whatever its size
        var shipmentIds = entries.Select(x => x.ObjectId).Distinct().ToList();
        var shipments = (await shipmentRepository.ToListAsync(shipmentRepository.Table.Where(x =>
            shipmentIds.Contains(x.Id)))).ToDictionary(x => x.Id);
        var orderIds = shipments.Values.Select(x => x.OrderId).Distinct().ToList();
        var orders = (await orderRepository.ToListAsync(orderRepository.Table.Where(x =>
            orderIds.Contains(x.Id)))).ToDictionary(x => x.Id);

        return new PagedList<BatchRecipient>(entries.Select(entry =>
        {
            var shipment = shipments.GetValueOrDefault(entry.ObjectId);
            var order = shipment == null ? null : orders.GetValueOrDefault(shipment.OrderId);
            return new BatchRecipient(entry.ObjectId, shipment?.ShipmentNumber ?? 0, shipment?.OrderId,
                order?.OrderNumber ?? 0, order?.CustomerId, order?.CustomerEmail, entry.OutQty - entry.InQty,
                shipment?.CreatedOnUtc ?? entry.CreateDateUtc);
        }).ToList(), pageIndex, pageSize, entries.TotalCount);
    }

    /// <summary>
    ///     The entries of the batch: the same number in another warehouse or combination is another batch, and the
    ///     journal spells the number as the batch does
    /// </summary>
    private IQueryable<InventoryJournal> BatchEntries(ProductBatch batch)
    {
        var combinationId = batch.CombinationId ?? "";
        var warehouseId = batch.WarehouseId ?? "";
        return journalRepository.Table.Where(x =>
            x.ProductId == batch.ProductId && x.BatchNumber == batch.Number && x.WarehouseId == warehouseId &&
            x.CombinationId == combinationId);
    }

    public async Task<IList<UnitBucketView>> Buckets(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var buckets = new List<UnitBucketView>();
        if (!InventoryTracking.Tracked(product))
            return buckets;

        //what the units hold in every bucket of the product, in one query
        var held = (product.InventoryTrackingModeId == InventoryTrackingMode.Batch
                ? await batchRepository.ToListAsync(batchRepository.Table
                    .Where(x => x.ProductId == product.Id && x.Quantity != 0)
                    .GroupBy(x => new { x.CombinationId, x.WarehouseId })
                    .Select(g => new UnitCount {
                        CombinationId = g.Key.CombinationId, WarehouseId = g.Key.WarehouseId,
                        Quantity = g.Sum(x => x.Quantity)
                    }))
                : await serialNumberRepository.ToListAsync(serialNumberRepository.Table
                    .Where(x => x.ProductId == product.Id && x.StatusId == SerialNumberStatus.InStock)
                    .GroupBy(x => new { x.CombinationId, x.WarehouseId })
                    .Select(g => new UnitCount {
                        CombinationId = g.Key.CombinationId, WarehouseId = g.Key.WarehouseId, Quantity = g.Count()
                    })))
            .GroupBy(x => (x.CombinationId ?? "", x.WarehouseId ?? ""))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));

        IEnumerable<string> warehouses = product.UseMultipleWarehouses ? StockBuckets.Warehouses(product) : [product.WarehouseId];
        foreach (var warehouseId in warehouses)
        foreach (var (combinationId, attributes, stock) in StockBuckets.In(product, warehouseId))
        {
            var unitWarehouse = StockBuckets.UnitWarehouse(product, warehouseId);
            var combination = combinationId ?? "";
            buckets.Add(new UnitBucketView(combination, attributes, unitWarehouse, stock,
                held.GetValueOrDefault((combination, unitWarehouse ?? ""))));
        }

        return buckets;
    }

    /// <summary>
    ///     The units one bucket holds, as the database groups them
    /// </summary>
    private sealed class UnitCount
    {
        public string CombinationId { get; set; }
        public string WarehouseId { get; set; }
        public int Quantity { get; set; }
    }

    public async Task<IList<BatchAllocation>> ProposeBatches(Product product, IList<CustomAttribute> attributes,
        string warehouseId, int quantity, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(product);

        var (combinationId, stock) = Bucket(product, attributes, warehouseId);
        var batches = (await Batches(product.Id, combinationId, StockBuckets.UnitWarehouse(product, warehouseId)))
            .Where(x => x.Quantity > 0).ToList();
        var warningDays = BatchExpiry.WarningDays(product, await settingService.LoadSetting<InventorySettings>());

        var allocations = new List<BatchAllocation>();
        var remaining = quantity;
        //Batches came ordered by expiry, those without one last; expired ones go after every other batch
        foreach (var expired in new[] { false, true })
        foreach (var batch in batches)
        {
            var status = BatchExpiry.Status(batch.ExpiryDateUtc, today, warningDays);
            if ((status == BatchExpiryStatus.Expired) != expired)
                continue;

            var taken = Math.Min(remaining, batch.Quantity);
            remaining -= taken;
            allocations.Add(new BatchAllocation(batch.Number, batch.ExpiryDateUtc, status, batch.Quantity, taken));
        }

        var pool = Math.Max(0, stock - batches.Sum(x => x.Quantity));
        var fromPool = product.BackorderModeId == BackorderMode.NoBackorders ? Math.Min(remaining, pool) : remaining;
        if (pool > 0 || fromPool > 0)
            allocations.Add(new BatchAllocation("", null, null, pool, fromPool));
        return allocations;
    }

    public async Task<IList<ShipmentUnitError>> PrepareShipmentUnits(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        //the repository stamps the creation date on insert: a shipment not stored yet is created now
        var createdOnUtc = shipment.CreatedOnUtc == default ? DateTime.UtcNow : shipment.CreatedOnUtc;
        var errors = new List<ShipmentUnitError>();
        var named = new Dictionary<string, HashSet<string>>();
        var booked = new Dictionary<string, List<StockChange>>();
        foreach (var item in shipment.ShipmentItems)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                continue;

            string error;
            if (InventoryTracking.Tracked(product))
            {
                if (product.WarrantyMonths > 0)
                    item.WarrantyUntilUtc = createdOnUtc.AddMonths(product.WarrantyMonths);
                error = product.InventoryTrackingModeId == InventoryTrackingMode.Batch
                    ? BatchError(item)
                    : await SerialNumberError(product, item, named);
                error ??= await LedgerError(product, ShipmentStock.Changes(product, shipment, item), booked);
            }
            else
            {
                error = await TrackedChildError(product, shipment, item, booked);
            }

            if (error != null)
                errors.Add(new ShipmentUnitError(item.Id, product.Name, error));
        }

        return errors;
    }

    /// <summary>
    ///     The first error the ledger finds booking the changes after those of the items checked before them, so each
    ///     item is checked against what those leave; the changes of an item without errors join them
    /// </summary>
    /// <param name="booked">The changes of the items checked so far, by product</param>
    private async Task<string> LedgerError(Product product, List<StockChange> changes,
        Dictionary<string, List<StockChange>> booked)
    {
        List<StockChange> all = [..booked.GetValueOrDefault(product.Id) ?? [], ..changes];
        //read again for every check: the ledger works the changes out on the product it is given
        var errors = await stockLedger.Check(await productRepository.GetByIdAsync(product.Id), all);
        if (errors.Count > 0)
            return errors[0];

        booked[product.Id] = all;
        return null;
    }

    /// <summary>
    ///     The combination of a bucket and the stock it holds
    /// </summary>
    private static (string CombinationId, int Stock) Bucket(Product product, IList<CustomAttribute> attributes,
        string warehouseId)
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            var combination = product.FindProductAttributeCombination(attributes ?? []);
            if (combination == null)
                return ("", 0);
            return (combination.Id, product.UseMultipleWarehouses
                ? combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId)?.StockQuantity ?? 0
                : combination.StockQuantity);
        }

        return ("", product.UseMultipleWarehouses
            ? product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId)?.StockQuantity ?? 0
            : product.StockQuantity);
    }

    /// <summary>
    ///     The batches a shipment item names cover its quantity at most: the rest comes from the stock without one
    /// </summary>
    private static string BatchError(ShipmentItem item)
    {
        return (item.Batches ?? []).Where(x => x.Quantity > 0).Sum(x => x.Quantity) > item.Quantity
            ? UnitErrors.BatchQuantityExceeded
            : null;
    }

    /// <summary>
    ///     A serial number is named once a shipment; a known one is named as it was received
    /// </summary>
    /// <param name="named">The serial numbers the shipment's items named so far, by product</param>
    private async Task<string> SerialNumberError(Product product, ShipmentItem item,
        Dictionary<string, HashSet<string>> named)
    {
        var numbers = item.SerialNumbers ?? [];
        if (!named.TryGetValue(product.Id, out var keys))
            named[product.Id] = keys = [];
        if (numbers.Any(x => !keys.Add(UnitNumbers.Key(x))))
            return UnitErrors.SerialDuplicate;

        var itemKeys = numbers.Select(UnitNumbers.Key).ToList();
        var stored = await serialNumberRepository.ToListAsync(serialNumberRepository.Table.Where(x =>
            x.ProductId == product.Id && itemKeys.Contains(x.NumberKey)));
        //the shipment and the order views name a known unit as it was received
        item.SerialNumbers = numbers.Select(x => stored.FirstOrDefault(s => s.NumberKey == UnitNumbers.Key(x))?.Number ??
                                                 x.Trim()).ToList();
        return null;
    }

    /// <summary>
    ///     The products of a bundle, and those associated with the attributes chosen, ship from their stock without
    ///     units: never one tracking serial numbers, and one tracking batches only while the ledger books it
    /// </summary>
    private async Task<string> TrackedChildError(Product product, Shipment shipment, ShipmentItem item,
        Dictionary<string, List<StockChange>> booked)
    {
        var children = new List<(string ProductId, int Quantity)>();
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            children.AddRange(product.BundleProducts.Select(x => (x.ProductId, x.Quantity * item.Quantity)));
        children.AddRange(product.ParseProductAttributeValues(item.Attributes ?? [])
            .Where(x => x.AttributeValueTypeId == AttributeValueType.AssociatedToProduct)
            .Select(x => (x.AssociatedProductId, x.Quantity * item.Quantity)));

        foreach (var (childId, quantity) in children)
        {
            var child = await productRepository.GetByIdAsync(childId);
            if (child == null || child.Id == product.Id || !InventoryTracking.Tracked(child))
                continue;
            if (child.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber)
                return UnitErrors.TrackedBundleChild;

            //booked as the booking books it: the item's attributes and warehouse, the child's quantity
            var childItem = new ShipmentItem {
                Id = item.Id, Attributes = item.Attributes, Quantity = quantity, WarehouseId = item.WarehouseId
            };
            if (await LedgerError(child, ShipmentStock.Changes(child, shipment, childItem), booked) != null)
                return UnitErrors.TrackedBundleChild;
        }

        return null;
    }

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
