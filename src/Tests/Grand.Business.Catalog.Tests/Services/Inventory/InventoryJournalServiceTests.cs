using Grand.Business.Catalog.Services.Inventory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class InventoryJournalServiceTests
{
    private IRepository<InventoryJournal> _journal;
    private IRepository<Product> _products;
    private InventoryJournalService _service;

    [TestInitialize]
    public void Init()
    {
        _products = new MongoDBRepositoryTest<Product>();
        _journal = new MongoDBRepositoryTest<InventoryJournal>();
        _service = new InventoryJournalService(_journal, _products);
    }

    private Task Entry(string productId, DateTime date, int inQty, InventoryMovementType type,
        string warehouseId = "w1", string storeId = "store1", int outQty = 0)
    {
        return _journal.InsertAsync(new InventoryJournal {
            ProductId = productId, CreateDateUtc = date, InQty = inQty, OutQty = outQty, MovementTypeId = type,
            WarehouseId = warehouseId, StoreId = storeId, CombinationId = ""
        });
    }

    [TestMethod]
    public async Task GetProductHistory_NewestFirstAndPaged()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
            await Entry("p1", now.AddMinutes(i), i, InventoryMovementType.Receipt);
        await Entry("p2", now, 1, InventoryMovementType.Receipt);

        var page = await _service.GetProductHistory("p1", null, null, null, null, 0, 2);

        Assert.AreEqual(5, page.TotalCount);
        CollectionAssert.AreEqual(new[] { 4, 3 }, page.Select(x => x.InQty).ToArray());
    }

    [TestMethod]
    public async Task GetProductHistory_StoreFilter_IncludesShared()
    {
        var now = DateTime.UtcNow;
        await Entry("p1", now, 1, InventoryMovementType.Receipt, storeId: "store1");
        await Entry("p1", now, 2, InventoryMovementType.Receipt, storeId: "");
        await Entry("p1", now, 3, InventoryMovementType.Receipt, storeId: "store2");
        //written before entries named their store
        await Entry("p1", now, 4, InventoryMovementType.Legacy, storeId: null);

        var page = await _service.GetProductHistory("p1", null, null, null, ["store1", ""], 0, 10);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, page.Select(x => x.InQty).ToArray());
    }

    [TestMethod]
    public async Task GetStockAtDate_SubtractsLaterEntries()
    {
        var product = new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1", StockQuantity = 7 };
        await _products.InsertAsync(product);
        var start = DateTime.UtcNow.AddDays(-10);
        await Entry(product.Id, start, 4, InventoryMovementType.OpeningBalance);
        await Entry(product.Id, start.AddDays(2), 5, InventoryMovementType.Receipt);
        await Entry(product.Id, start.AddDays(5), 0, InventoryMovementType.Shipment, outQty: 2);

        var rows = await _service.GetStockAtDate("w1", start.AddDays(3), null);

        var row = rows.Single();
        Assert.AreEqual(product.Id, row.ProductId);
        Assert.AreEqual(9, row.Stock);
        Assert.IsTrue(row.Available);
    }

    [TestMethod]
    public async Task GetStockAtDate_BeforeOpeningBalance_NotAvailable()
    {
        var product = new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1", StockQuantity = 4 };
        await _products.InsertAsync(product);
        var start = DateTime.UtcNow.AddDays(-10);
        await Entry(product.Id, start, 4, InventoryMovementType.OpeningBalance);

        var rows = await _service.GetStockAtDate("w1", start.AddDays(-1), null);

        Assert.IsFalse(rows.Single().Available);
    }

    [TestMethod]
    public async Task GetStockAtDate_OnlyBucketsOfTheWarehouse()
    {
        await _products.InsertAsync(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w2", StockQuantity = 4 });
        await _products.InsertAsync(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 3 } }
        });

        var rows = await _service.GetStockAtDate("w1", DateTime.UtcNow, null);

        Assert.AreEqual(3, rows.Single().Stock);
    }

    [TestMethod]
    public async Task GetStockAtDate_Store_LeavesOutProductsLimitedToOtherStores()
    {
        foreach (var (limited, stores) in new[] { (false, new string[0]), (true, ["store1"]), (true, ["store2"]) })
            await _products.InsertAsync(new Product {
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1", StockQuantity = 1,
                LimitedToStores = limited, Stores = stores.ToList()
            });

        var rows = await _service.GetStockAtDate("w1", DateTime.UtcNow, "store1");

        Assert.AreEqual(2, rows.Count);
    }
}
