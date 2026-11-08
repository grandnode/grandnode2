using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

/// <summary>
///     A ledger over in-memory repositories, for the tests of batches and serial numbers
/// </summary>
public sealed class UnitTestFixture
{
    public IRepository<Product> Products { get; } = new MongoDBRepositoryTest<Product>();
    public IRepository<InventoryJournal> Journal { get; } = new MongoDBRepositoryTest<InventoryJournal>();
    public IRepository<ProductBatch> Batches { get; } = new MongoDBRepositoryTest<ProductBatch>();
    public IRepository<ProductSerialNumber> Serials { get; } = new MongoDBRepositoryTest<ProductSerialNumber>();
    public IRepository<Shipment> Shipments { get; } = new MongoDBRepositoryTest<Shipment>();

    public StockLedger Ledger =>
        StockTestFixture.Ledger(Products, Journal, batches: Batches, serials: Serials, shipments: Shipments);

    public Product Stored(Product product) => Products.Table.First(x => x.Id == product.Id);

    public Task<IList<string>> Apply(Product product, params StockChange[] changes) =>
        Ledger.Apply(Stored(product), changes);

    public static StockChange Change(int quantity, InventoryMovementType type, string warehouseId = "",
        IList<CustomAttribute> attributes = null) =>
        new(attributes ?? [], warehouseId, quantity, false, type, new StockSource("InventoryDocument", "d1", "l1"));

    /// <summary>
    ///     Inserts the product with its stock journaled while it tracked no units, then switches the tracking on: the
    ///     stock becomes stock without units
    /// </summary>
    public async Task<Product> Stocked(Product product, InventoryTrackingMode mode,
        params (string WarehouseId, IList<CustomAttribute> Attributes, int Quantity)[] stock)
    {
        product.InventoryTrackingModeId = InventoryTrackingMode.None;
        await Products.InsertAsync(product);
        if (stock.Length > 0)
            Assert.AreEqual(0, (await Apply(product, stock.Select(x =>
                Change(x.Quantity, InventoryMovementType.ManualEdit, x.WarehouseId, x.Attributes)).ToArray())).Count);

        var stored = Stored(product);
        stored.InventoryTrackingModeId = mode;
        await Products.UpdateAsync(stored);
        return stored;
    }

    public List<ProductBatch> BatchesOf(Product product) =>
        Batches.Table.Where(x => x.ProductId == product.Id).ToList();

    public List<ProductSerialNumber> SerialsOf(Product product) =>
        Serials.Table.Where(x => x.ProductId == product.Id).ToList();

    public List<InventoryJournal> Entries(Product product) =>
        Journal.Table.Where(x => x.ProductId == product.Id).OrderBy(x => x.StockRevision).ToList();

    /// <summary>
    ///     Every bucket's stock is the sum of its journal; every batch holds what the journal moved in and out of it,
    ///     never less than nothing; in-stock serial numbers never exceed the stock
    /// </summary>
    public void AssertInStep(Product product)
    {
        var stored = Stored(product);
        foreach (var (combinationId, warehouseId, stock) in Buckets(stored))
        {
            Assert.AreEqual(stock, StockTestFixture.JournalSum(Journal, stored.Id, combinationId, warehouseId),
                $"journal of bucket ({combinationId}, {warehouseId})");
            Assert.IsTrue(stock - SerialsOf(stored).Count(x => x.StatusId == SerialNumberStatus.InStock &&
                                                                (x.CombinationId ?? "") == combinationId &&
                                                                (x.WarehouseId ?? "") == warehouseId) >= 0,
                "serial numbers exceed the stock");
        }

        foreach (var batch in BatchesOf(stored))
        {
            Assert.IsTrue(batch.Quantity >= 0, $"batch {batch.Number} below zero");
            var moved = Entries(stored)
                .Where(x => UnitNumbers.Key(x.BatchNumber) == batch.NumberKey &&
                            (x.CombinationId ?? "") == (batch.CombinationId ?? "") &&
                            (x.WarehouseId ?? "") == (batch.WarehouseId ?? ""))
                .Sum(x => x.MovementTypeId == InventoryMovementType.TrackingAssignment ? x.InQty : x.InQty - x.OutQty);
            Assert.AreEqual(moved, batch.Quantity, $"journal of batch {batch.Number}");
        }
    }

    private static IEnumerable<(string, string, int)> Buckets(Product product)
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
        {
            if (!product.UseMultipleWarehouses)
                yield return ("", product.WarehouseId ?? "", product.StockQuantity);
            else
                foreach (var row in product.ProductWarehouseInventory)
                    yield return ("", row.WarehouseId, row.StockQuantity);
            yield break;
        }

        foreach (var combination in product.ProductAttributeCombinations)
            if (!product.UseMultipleWarehouses)
                yield return (combination.Id, product.WarehouseId ?? "", combination.StockQuantity);
            else
                foreach (var row in combination.WarehouseInventory)
                    yield return (combination.Id, row.WarehouseId, row.StockQuantity);
    }
}
