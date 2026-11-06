using Grand.Data.LiteDb;
using Grand.Data.Tests;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Module.Migration.Migrations._2._5;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationInventoryLedgerTests
{
    private LiteDatabase _database;
    private LiteDBRepository<InventoryJournal> _journal;
    private LiteDBRepository<Product> _products;
    private LiteDBRepository<Warehouse> _warehouses;

    [TestInitialize]
    public async Task Setup()
    {
        _database = new LiteDatabase(new MemoryStream());
        _products = new LiteDBRepository<Product>(_database, new AuditInfoProvider());
        _journal = new LiteDBRepository<InventoryJournal>(_database, new AuditInfoProvider());
        _warehouses = new LiteDBRepository<Warehouse>(_database, new AuditInfoProvider());
        await _warehouses.InsertAsync(new Warehouse { Id = "w1", StoreId = "store1" });
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    private async Task Migrate()
    {
        await MigrationInventoryLedger.MarkLegacy(_journal);
        await MigrationInventoryLedger.OpenBalances(_products, _journal, _warehouses);
    }

    [TestMethod]
    public async Task Migration_MarksExistingRowsLegacy_AndWritesOpeningBalancePerBucket()
    {
        var product = new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1", StockQuantity = 7 };
        await _products.InsertAsync(product);
        //a shipment booked before the upgrade: it carries no movement type
        await _journal.InsertAsync(new InventoryJournal
            { ProductId = product.Id, ObjectType = "Shipment", PositionId = "item", OutQty = 3 });
        await _products.InsertAsync(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.DontManageStock, StockQuantity = 5 });

        await Migrate();

        var entries = _journal.Table.ToList();
        Assert.AreEqual(InventoryMovementType.Legacy, entries.Single(x => x.ObjectType == "Shipment").MovementTypeId);
        var opening = entries.Single(x => x.MovementTypeId == InventoryMovementType.OpeningBalance);
        Assert.AreEqual(product.Id, opening.ProductId);
        Assert.AreEqual(7, opening.InQty);
        Assert.AreEqual("w1", opening.WarehouseId);
        Assert.AreEqual("store1", opening.StoreId);
        Assert.AreEqual(1, opening.StockRevision);
        Assert.AreEqual(1, _products.Table.First(x => x.Id == product.Id).StockRevision);
    }

    [TestMethod]
    public async Task Migration_MultiWarehouseWithStaleSum_RecomputesSum()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            StockQuantity = 99,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 4 },
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = -1 }
            }
        };
        await _products.InsertAsync(product);

        await Migrate();

        Assert.AreEqual(3, _products.Table.First(x => x.Id == product.Id).StockQuantity);
        var openings = _journal.Table.ToList().OrderBy(x => x.WarehouseId).ToList();
        Assert.AreEqual(2, openings.Count);
        Assert.AreEqual(4, openings[0].InQty);
        Assert.AreEqual(1, openings[1].OutQty);
    }

    [TestMethod]
    public async Task Migration_Combinations_OneOpeningBalancePerCombination()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            ProductAttributeCombinations = {
                new ProductAttributeCombination { StockQuantity = 2 },
                new ProductAttributeCombination { StockQuantity = 5 }
            }
        };
        await _products.InsertAsync(product);

        await Migrate();

        CollectionAssert.AreEquivalent(product.ProductAttributeCombinations.Select(x => x.Id).ToArray(),
            _journal.Table.ToList().Select(x => x.CombinationId).ToArray());
        Assert.AreEqual(7, _products.Table.First(x => x.Id == product.Id).StockQuantity);
    }

    [TestMethod]
    public async Task Migration_RunTwice_WritesOpeningBalanceOnce()
    {
        await _products.InsertAsync(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7 });

        await Migrate();
        await Migrate();

        Assert.AreEqual(1, _journal.Table.Count());
    }
}
