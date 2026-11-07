using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Catalog.Services.Inventory;

public class ProductUnitService(
    IRepository<ProductBatch> batchRepository,
    IRepository<ProductSerialNumber> serialNumberRepository,
    IRepository<Product> productRepository,
    ISettingService settingService) : IProductUnitService
{
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
                    ? await BatchError(product, item)
                    : await SerialNumberError(product, item, named);
            }
            else
            {
                error = await TrackedChildError(product, item);
            }

            if (error != null)
                errors.Add(new ShipmentUnitError(item.Id, product.Name, error));
        }

        return errors;
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

    private async Task<string> BatchError(Product product, ShipmentItem item)
    {
        var (combinationId, stock) = Bucket(product, item.Attributes, item.WarehouseId);
        var batches = await Batches(product.Id, combinationId, StockBuckets.UnitWarehouse(product, item.WarehouseId));
        var taken = 0;
        foreach (var batch in (item.Batches ?? []).GroupBy(x => UnitNumbers.Key(x.Number)))
        {
            var stored = batches.FirstOrDefault(x => x.NumberKey == batch.Key);
            if (stored == null)
                return UnitErrors.BatchNotFound;
            if (stored.Quantity < batch.Sum(x => x.Quantity))
                return UnitErrors.BatchQuantityExceeded;
            taken += batch.Sum(x => x.Quantity);
        }

        if (taken > item.Quantity)
            return UnitErrors.BatchQuantityExceeded;

        var pool = stock - batches.Sum(x => x.Quantity);
        return item.Quantity - taken > Math.Max(0, pool) && product.BackorderModeId == BackorderMode.NoBackorders
            ? UnitErrors.PoolExceeded
            : null;
    }

    /// <summary>
    ///     A unit with a serial number ships with it: in stock in the warehouse shipped from, or new for the product
    ///     while there is stock without numbers to take it from
    /// </summary>
    /// <param name="named">The serial numbers the shipment's items named so far, by product</param>
    private async Task<string> SerialNumberError(Product product, ShipmentItem item,
        Dictionary<string, HashSet<string>> named)
    {
        var numbers = item.SerialNumbers ?? [];
        if (numbers.Count != item.Quantity)
            return UnitErrors.SerialCountMismatch;

        if (!named.TryGetValue(product.Id, out var keys))
            named[product.Id] = keys = [];
        if (numbers.Any(x => !keys.Add(UnitNumbers.Key(x))))
            return UnitErrors.SerialDuplicate;

        var (combinationId, stock) = Bucket(product, item.Attributes, item.WarehouseId);
        var warehouseId = StockBuckets.UnitWarehouse(product, item.WarehouseId);
        var itemKeys = numbers.Select(UnitNumbers.Key).ToList();
        var stored = await serialNumberRepository.ToListAsync(serialNumberRepository.Table.Where(x =>
            x.ProductId == product.Id && itemKeys.Contains(x.NumberKey)));
        var added = 0;
        foreach (var key in itemKeys)
            switch (stored.FirstOrDefault(x => x.NumberKey == key))
            {
                case { StatusId: SerialNumberStatus.InStock } here when (here.CombinationId ?? "") == combinationId &&
                                                                       (here.WarehouseId ?? "") == warehouseId:
                    break;
                case { StatusId: SerialNumberStatus.InStock }:
                    return UnitErrors.SerialInOtherWarehouse;
                case { StatusId: SerialNumberStatus.Shipped }:
                    return UnitErrors.SerialShipped;
                default:
                    added++;
                    break;
            }

        if (added == 0)
            return null;

        var inStock = await serialNumberRepository.CountAsync(serialNumberRepository.Table.Where(x =>
            x.ProductId == product.Id && x.CombinationId == combinationId && x.WarehouseId == warehouseId &&
            x.StatusId == SerialNumberStatus.InStock));
        return added > stock - inStock ? UnitErrors.PoolExceeded : null;
    }

    /// <summary>
    ///     The products of a bundle, and those associated with the attributes chosen, ship from their stock without
    ///     units: never one tracking serial numbers, and one tracking batches only while that stock covers it or it
    ///     allows backorders
    /// </summary>
    private async Task<string> TrackedChildError(Product product, ShipmentItem item)
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

            var (combinationId, stock) = Bucket(child, [], item.WarehouseId);
            var batches = await Batches(child.Id, combinationId, StockBuckets.UnitWarehouse(child, item.WarehouseId));
            if (quantity > stock - batches.Sum(x => x.Quantity) && child.BackorderModeId == BackorderMode.NoBackorders)
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
