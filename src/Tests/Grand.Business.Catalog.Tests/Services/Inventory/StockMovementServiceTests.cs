using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Data.Tests.LiteDb;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockMovementServiceTests
{
    private static async Task ParallelChangesEndWithExactSum(IRepository<Product> products,
        IRepository<InventoryJournal> journal, IResourceLock resourceLock)
    {
        var service = StockTestFixture.MovementService(products, journal, resourceLock);
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock };
        await products.InsertAsync(product);
        var change = new StockChange([], "", 1, false, InventoryMovementType.ManualEdit, new StockSource("Product"));

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => service.Apply(product.Id, [change]))));

        var stored = products.Table.First(x => x.Id == product.Id);
        var revisions = journal.Table.Where(x => x.ProductId == product.Id).ToList().Select(x => x.StockRevision)
            .OrderBy(x => x).ToArray();
        Assert.AreEqual(20, stored.StockQuantity);
        Assert.AreEqual(20, stored.StockRevision);
        CollectionAssert.AreEqual(Enumerable.Range(1, 20).ToArray(), revisions);
    }

    [TestMethod]
    public async Task Apply_ParallelChangesOnOneBucket_EndWithExactSum()
    {
        await ParallelChangesEndWithExactSum(new MongoDBRepositoryTest<Product>(),
            new MongoDBRepositoryTest<InventoryJournal>(), new MongoResourceLockTest());
    }

    [TestMethod]
    public async Task Apply_ParallelChangesOnOneBucket_LiteDb_EndWithExactSum()
    {
        using var lockDatabase = new LiteDatabase(new MemoryStream());
        await ParallelChangesEndWithExactSum(new LiteDBRepositoryMock<Product>(),
            new LiteDBRepositoryMock<InventoryJournal>(), new LiteDBResourceLock(() => lockDatabase));
    }

    [TestMethod]
    public async Task Apply_UnknownProduct_Refused()
    {
        var products = new MongoDBRepositoryTest<Product>();
        var service = StockTestFixture.MovementService(products, new MongoDBRepositoryTest<InventoryJournal>(),
            new MongoResourceLockTest());

        var errors = await service.Apply("missing",
            [new StockChange([], "", 1, false, InventoryMovementType.ManualEdit, new StockSource())]);

        CollectionAssert.AreEqual(new[] { StockErrors.ProductNotFound }, errors.ToArray());
    }

    [TestMethod]
    public void InitialEntries_MultiWarehouse_OneEntryPerNonZeroRow()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 3 },
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 0 }
            }
        };

        var entries = StockBuckets.InitialEntries(product, InventoryMovementType.ManualEdit,
            new StockSource("Product", CustomerId: "staff"), new Dictionary<string, string> { ["w1"] = "store1" });

        var entry = entries.Single();
        Assert.AreEqual("w1", entry.WarehouseId);
        Assert.AreEqual("store1", entry.StoreId);
        Assert.AreEqual(3, entry.InQty);
        Assert.AreEqual("staff", entry.CustomerId);
        Assert.AreEqual(1, entry.StockRevision);
        Assert.AreEqual(1, product.StockRevision);
    }

    [TestMethod]
    public void InitialEntries_DontManageStock_None()
    {
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.DontManageStock, StockQuantity = 4 };

        var entries = StockBuckets.InitialEntries(product, InventoryMovementType.ManualEdit, new StockSource(),
            new Dictionary<string, string>());

        Assert.AreEqual(0, entries.Count);
        Assert.AreEqual(0, product.StockRevision);
    }

    [TestMethod]
    public async Task JournalNewProduct_SumEqualsInsertedStock()
    {
        var products = new MongoDBRepositoryTest<Product>();
        var journal = new MongoDBRepositoryTest<InventoryJournal>();
        var service = StockTestFixture.MovementService(products, journal, new MongoResourceLockTest());
        var product = StockTestFixture.WithCombinations(false, 3, 4);

        await service.JournalNewProduct(product, InventoryMovementType.Import, new StockSource("Import"));
        await products.InsertAsync(product);

        var stored = products.Table.First(x => x.Id == product.Id);
        Assert.AreEqual(2, stored.StockRevision);
        foreach (var combination in stored.ProductAttributeCombinations)
            Assert.AreEqual(combination.StockQuantity,
                StockTestFixture.JournalSum(journal, product.Id, combination.Id, ""));
        Assert.IsTrue(journal.Table.All(x => x.MovementTypeId == InventoryMovementType.Import));
    }
}
