using Grand.Business.Catalog.Events.Handlers;
using Grand.Business.Catalog.Tests.Services.Inventory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class WarehouseDeletedEventHandlerTests
{
    private Mock<ICacheBase> _cacheMock;
    private WarehouseDeletedEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _cacheMock = new Mock<ICacheBase>();
        _journal = new MongoDBRepositoryTest<InventoryJournal>();
        _handler = new WarehouseDeletedEventHandler(_repository, _cacheMock.Object, new MongoResourceLockTest(),
            StockTestFixture.Ledger(_repository, _journal));
    }

    private IRepository<InventoryJournal> _journal;

    [TestMethod]
    public async Task WarehouseDeleted_RowWithStock_WritesAdjustmentOutAndRemovesRow()
    {
        var warehouse = new Warehouse();
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            StockQuantity = 8,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = warehouse.Id, StockQuantity = 5 },
                new ProductWarehouseInventory { WarehouseId = "other", StockQuantity = 3 }
            }
        };
        await _repository.InsertAsync(product);

        await _handler.Handle(new EntityDeleted<Warehouse>(warehouse), CancellationToken.None);

        var stored = _repository.Table.Single(x => x.Id == product.Id);
        Assert.AreEqual("other", stored.ProductWarehouseInventory.Single().WarehouseId);
        Assert.AreEqual(3, stored.StockQuantity);
        var entry = _journal.Table.Single(x => x.ProductId == product.Id);
        Assert.AreEqual(InventoryMovementType.Adjustment, entry.MovementTypeId);
        Assert.AreEqual(5, entry.OutQty);
        Assert.AreEqual(warehouse.Id, entry.WarehouseId);
        Assert.AreEqual("Warehouse deleted", entry.Comments);
    }

    [TestMethod]
    public async Task WarehouseDeleted_ProductKeptThere_MovesItsHistoryOutOfTheWarehouse()
    {
        var warehouse = new Warehouse();
        var product = new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = warehouse.Id, StockQuantity = 4 };
        await _repository.InsertAsync(product);

        await _handler.Handle(new EntityDeleted<Warehouse>(warehouse), CancellationToken.None);

        var stored = _repository.Table.Single(x => x.Id == product.Id);
        Assert.AreEqual("", stored.WarehouseId);
        Assert.AreEqual(4, stored.StockQuantity);
        var entries = _journal.Table.Where(x => x.ProductId == product.Id).ToList();
        Assert.AreEqual(4, entries.Single(x => x.MovementTypeId == InventoryMovementType.ClosingBalance &&
                                               x.WarehouseId == warehouse.Id).OutQty);
        Assert.AreEqual(4, entries.Single(x => x.MovementTypeId == InventoryMovementType.OpeningBalance &&
                                               x.WarehouseId == "").InQty);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var warehouse = new Warehouse();
        var product = new Product {
            WarehouseId = warehouse.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            WarehouseId = warehouse.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            WarehouseId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<Warehouse>(warehouse), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.WarehouseId == warehouse.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.WarehouseId == "1").Count());
        _cacheMock.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RemovesTheWarehouseFromProductInventory()
    {
        var warehouse = new Warehouse();
        var product = new Product {
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = warehouse.Id, StockQuantity = 5 },
                new ProductWarehouseInventory { WarehouseId = "other", StockQuantity = 3 }
            }
        };
        await _repository.InsertAsync(product);

        await _handler.Handle(new EntityDeleted<Warehouse>(warehouse), CancellationToken.None);

        var inventory = _repository.Table.Single(x => x.Id == product.Id).ProductWarehouseInventory;
        Assert.AreEqual(1, inventory.Count);
        Assert.AreEqual("other", inventory.Single().WarehouseId);
    }

    [TestMethod]
    public async Task Handle_LeavesProductsWithoutTheWarehouseUntouched()
    {
        //deleting one warehouse must not rewrite (and re-stamp UpdatedOnUtc/UpdatedBy on) the whole catalog
        var warehouse = new Warehouse();
        var unrelated = new Product {
            WarehouseId = "other",
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "other", StockQuantity = 3 } }
        };
        await _repository.InsertAsync(unrelated);
        var stampedBefore = _repository.Table.Single(x => x.Id == unrelated.Id).UpdatedOnUtc;

        await _handler.Handle(new EntityDeleted<Warehouse>(warehouse), CancellationToken.None);

        var after = _repository.Table.Single(x => x.Id == unrelated.Id);
        Assert.AreEqual(stampedBefore, after.UpdatedOnUtc);
        Assert.AreEqual(1, after.ProductWarehouseInventory.Count);
    }
}