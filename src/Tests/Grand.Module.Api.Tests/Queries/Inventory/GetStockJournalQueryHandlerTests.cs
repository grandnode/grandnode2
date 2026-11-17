using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Module.Api.Queries.Handlers.Inventory;
using Grand.Module.Api.Queries.Models.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Module.Api.Tests.Queries.Inventory;

[TestClass]
public class GetStockJournalQueryHandlerTests
{
    //real repositories on the test database: the cursor and the filters must translate to a query
    private MongoDBRepositoryTest<InventoryJournal> _journalRepository;
    private MongoDBRepositoryTest<Product> _productRepository;
    private MongoDBRepositoryTest<Warehouse> _warehouseRepository;
    private GetStockJournalQueryHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _journalRepository = new MongoDBRepositoryTest<InventoryJournal>();
        _productRepository = new MongoDBRepositoryTest<Product>();
        _warehouseRepository = new MongoDBRepositoryTest<Warehouse>();
        _handler = new GetStockJournalQueryHandler(_journalRepository, _productRepository, _warehouseRepository);
    }

    private async Task<InventoryJournal> Entry(string warehouseId = "w1", string customerId = "erp",
        InventoryMovementType type = InventoryMovementType.Api, int secondsAgo = 60, string combinationId = null)
    {
        var entry = new InventoryJournal {
            Id = UniqueIdentifier.New,
            ProductId = "p1",
            CombinationId = combinationId,
            WarehouseId = warehouseId,
            CustomerId = customerId,
            MovementTypeId = type,
            InQty = 1,
            CreateDateUtc = DateTime.UtcNow.AddSeconds(-secondsAgo)
        };
        await _journalRepository.InsertAsync(entry);
        return entry;
    }

    [TestMethod]
    public async Task Handle_PagesWithCursor_EveryEntryOnceAndCursorKeptAtTheEnd()
    {
        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
            ids.Add((await Entry()).Id);

        var first = await _handler.Handle(new GetStockJournalQuery { Take = 2 }, default);
        var second = await _handler.Handle(new GetStockJournalQuery { After = first.Next, Take = 2 }, default);
        var third = await _handler.Handle(new GetStockJournalQuery { After = second.Next, Take = 2 }, default);
        var later = await _handler.Handle(new GetStockJournalQuery { After = third.Next, Take = 2 }, default);

        CollectionAssert.AreEqual(ids,
            first.Entries.Concat(second.Entries).Concat(third.Entries).Select(x => x.Id).ToList());
        Assert.IsTrue(first.HasMore);
        Assert.IsTrue(second.HasMore);
        Assert.IsFalse(third.HasMore);
        //the last page hands back its last id, not an empty cursor that would read from the beginning
        Assert.AreEqual(ids[^1], third.Next);
        Assert.AreEqual(0, later.Entries.Count);
        Assert.AreEqual(ids[^1], later.Next);
    }

    [TestMethod]
    public async Task Handle_EntryInsideSettleWindow_NotReturnedYet()
    {
        var settled = await Entry();
        await Entry(secondsAgo: 5);

        var result = await _handler.Handle(new GetStockJournalQuery(), default);

        Assert.AreEqual(1, result.Entries.Count);
        Assert.AreEqual(settled.Id, result.Entries[0].Id);
    }

    [TestMethod]
    public async Task Handle_WarehouseCode_OnlyThatWarehouseWithItsCode()
    {
        await _warehouseRepository.InsertAsync(new Warehouse { Id = "w1", Code = "WH-1" });
        await _warehouseRepository.InsertAsync(new Warehouse { Id = "w2", Code = "WH-2" });
        var own = await Entry("w1");
        await Entry("w2");

        var result = await _handler.Handle(new GetStockJournalQuery { WarehouseCode = "WH-1" }, default);

        Assert.AreEqual(1, result.Entries.Count);
        Assert.AreEqual(own.Id, result.Entries[0].Id);
        Assert.AreEqual("WH-1", result.Entries[0].WarehouseCode);
    }

    [TestMethod]
    public async Task Handle_UnknownWarehouseCode_ReturnsNothing()
    {
        await Entry("w1");

        var result = await _handler.Handle(new GetStockJournalQuery { WarehouseCode = "NONE", After = "x" },
            default);

        Assert.AreEqual(0, result.Entries.Count);
        Assert.AreEqual("x", result.Next);
    }

    [TestMethod]
    public async Task Handle_MovementTypes_OnlyThoseTypes()
    {
        await Entry(type: InventoryMovementType.Api);
        var shipment = await Entry(type: InventoryMovementType.Shipment);

        var result = await _handler.Handle(new GetStockJournalQuery {
            MovementTypes = [InventoryMovementType.Shipment]
        }, default);

        Assert.AreEqual(1, result.Entries.Count);
        Assert.AreEqual(shipment.Id, result.Entries[0].Id);
    }

    [TestMethod]
    public async Task Handle_ExcludeOwn_SkipsTheCallersEntries()
    {
        await Entry(customerId: "erp");
        var other = await Entry(customerId: "admin");

        var result = await _handler.Handle(new GetStockJournalQuery { ExcludeOwn = true, CustomerId = "erp" },
            default);

        Assert.AreEqual(1, result.Entries.Count);
        Assert.AreEqual(other.Id, result.Entries[0].Id);
    }

    [TestMethod]
    public async Task Handle_EntryOfCombination_CarriesTheCombinationSku()
    {
        await _productRepository.InsertAsync(new Product {
            Id = "p1", Sku = "P-1",
            ProductAttributeCombinations = { new ProductAttributeCombination { Id = "c1", Sku = "C-1" } }
        });
        await Entry(combinationId: "c1");
        await Entry();

        var result = await _handler.Handle(new GetStockJournalQuery(), default);

        Assert.AreEqual("C-1", result.Entries[0].Sku);
        Assert.AreEqual("P-1", result.Entries[1].Sku);
    }
}
