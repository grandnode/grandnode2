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
        var service = new StockMovementService(resourceLock, products, StockTestFixture.Ledger(products, journal));
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
        var service = new StockMovementService(new MongoResourceLockTest(), products,
            StockTestFixture.Ledger(products, new MongoDBRepositoryTest<InventoryJournal>()));

        var errors = await service.Apply("missing",
            [new StockChange([], "", 1, false, InventoryMovementType.ManualEdit, new StockSource())]);

        CollectionAssert.AreEqual(new[] { StockErrors.ProductNotFound }, errors.ToArray());
    }
}
