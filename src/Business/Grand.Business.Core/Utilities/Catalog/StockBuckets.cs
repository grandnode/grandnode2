using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     The stock buckets of a product, by its inventory method: the product, its combinations, or their warehouse rows
/// </summary>
public static class StockBuckets
{
    /// <summary>
    ///     The journal entries that record the stock a product starts with, one per bucket holding stock; written before
    ///     the product is inserted, they get revisions 1..n and the product gets revision n
    /// </summary>
    /// <param name="product">A product not inserted yet</param>
    /// <param name="movementType">What created the product</param>
    /// <param name="source">The source the entries name</param>
    /// <param name="warehouseStores">The store of each warehouse the buckets name</param>
    public static IList<InventoryJournal> InitialEntries(Product product, InventoryMovementType movementType,
        StockSource source, IReadOnlyDictionary<string, string> warehouseStores)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(warehouseStores);

        var entries = new List<InventoryJournal>();

        void Add(string combinationId, IList<CustomAttribute> attributes, string warehouseId, int stock)
        {
            if (stock == 0)
                return;

            warehouseId ??= "";
            entries.Add(new InventoryJournal {
                ProductId = product.Id,
                CombinationId = combinationId,
                Attributes = attributes,
                WarehouseId = warehouseId,
                StoreId = warehouseStores.GetValueOrDefault(warehouseId) ?? "",
                InQty = Math.Max(0, stock),
                OutQty = Math.Max(0, -stock),
                MovementTypeId = movementType,
                CreateDateUtc = DateTime.UtcNow,
                ObjectType = source.ObjectType,
                ObjectId = source.ObjectId,
                PositionId = source.PositionId,
                Reference = source.Reference,
                CustomerId = source.CustomerId,
                Comments = source.Comment,
                StockRevision = entries.Count + 1
            });
        }

        switch (product.ManageInventoryMethodId)
        {
            case ManageInventoryMethod.ManageStock when !product.UseMultipleWarehouses:
                Add("", [], product.WarehouseId, product.StockQuantity);
                break;
            case ManageInventoryMethod.ManageStock:
                foreach (var row in product.ProductWarehouseInventory)
                    Add("", [], row.WarehouseId, row.StockQuantity);
                break;
            case ManageInventoryMethod.ManageStockByAttributes:
                foreach (var combination in product.ProductAttributeCombinations)
                    if (!product.UseMultipleWarehouses)
                        Add(combination.Id, combination.Attributes, product.WarehouseId, combination.StockQuantity);
                    else
                        foreach (var row in combination.WarehouseInventory)
                            Add(combination.Id, combination.Attributes, row.WarehouseId, row.StockQuantity);
                break;
        }

        product.StockRevision = entries.Count;
        return entries;
    }

    /// <summary>
    ///     The warehouses the product's buckets name
    /// </summary>
    public static IEnumerable<string> Warehouses(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new[] { product.WarehouseId }
            .Concat(product.ProductWarehouseInventory.Select(x => x.WarehouseId))
            .Concat(product.ProductAttributeCombinations.SelectMany(x => x.WarehouseInventory)
                .Select(x => x.WarehouseId))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct();
    }
}
