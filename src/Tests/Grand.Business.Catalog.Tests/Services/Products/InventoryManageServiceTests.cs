using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class InventoryManageServiceTests
{
    private InventoryManageService _inventoryManageService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _repository;
    private IRepository<InventoryJournal> _repositoryInventoryJournal;
    private CatalogSettings _settings;
    private StockQuantityService _stockQuantityService;
    private Mock<ITranslationService> _translationService;
    private Mock<IWorkContext> _workContextMock;
    private Mock<IStoreContext> _storeContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _repositoryInventoryJournal = new MongoDBRepositoryTest<InventoryJournal>();
        _workContextMock = new Mock<IWorkContext>();
        _storeContextMock = new Mock<IStoreContext>();
        _translationService = new Mock<ITranslationService>();
        _storeContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _settings = new CatalogSettings();
        _stockQuantityService = new StockQuantityService();
        _inventoryManageService = new InventoryManageService(_repository, _repositoryInventoryJournal,
            _stockQuantityService, _mediatorMock.Object, _settings, new MongoResourceLockTest(),
            new StockSideEffects(_repository, new Mock<IOutOfStockSubscriptionService>().Object,
                _mediatorMock.Object, _settings));
    }

    private Product Stored(Product product)
    {
        return _repository.Table.First(x => x.Id == product.Id);
    }

    [TestMethod]
    public async Task TryReserve_ParallelOrdersForLastUnit_OnlyOneSucceeds()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 1
        };
        await _repository.InsertAsync(product);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            _inventoryManageService.TryReserve(product, 1, null, ""))));

        Assert.AreEqual(1, results.Count(x => x));
        Assert.AreEqual(1, Stored(product).ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_StaleProductObject_UsesStoredQuantities()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 1,
            ReservedQuantity = 1
        };
        await _repository.InsertAsync(product);
        product.ReservedQuantity = 0;

        Assert.IsFalse(await _inventoryManageService.TryReserve(product, 1, null, ""));
        Assert.AreEqual(1, Stored(product).ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_BackordersAllowed_ReservesBeyondStock()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.AllowQtyBelowZero,
            StockQuantity = 0
        };
        await _repository.InsertAsync(product);

        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 2, null, ""));
        Assert.AreEqual(2, Stored(product).ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_CombinationOutOfStock_ReturnsFalse()
    {
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination {
            StockQuantity = 0, AllowOutOfStockOrders = false
        });
        await _repository.InsertAsync(product);

        Assert.IsFalse(await _inventoryManageService.TryReserve(product, 1, new List<CustomAttribute>(), ""));
        Assert.AreEqual(0, Stored(product).ProductAttributeCombinations.First().ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_MultipleWarehouses_ChecksTheGivenWarehouse()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            UseMultipleWarehouses = true
        };
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 0 });
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 5 });
        await _repository.InsertAsync(product);

        Assert.IsFalse(await _inventoryManageService.TryReserve(product, 1, null, "w1"));
        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 1, null, "w2"));
        Assert.AreEqual(1, Stored(product).ProductWarehouseInventory.First(x => x.WarehouseId == "w2").ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_SingleWarehouseProductElsewhereThanTheGivenWarehouse_UsesItsStock()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            WarehouseId = "w1",
            StockQuantity = 5
        };
        await _repository.InsertAsync(product);

        //the order item falls back to the store's default warehouse w2
        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 1, null, "w2"));
        Assert.AreEqual(1, Stored(product).ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_SingleWarehouseCombinationElsewhereThanTheGivenWarehouse_UsesItsStock()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes, WarehouseId = "w1"
        };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { StockQuantity = 5 });
        await _repository.InsertAsync(product);

        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 1, new List<CustomAttribute>(), "w2"));
    }

    [TestMethod]
    public async Task TryReserve_BundleChildOutOfStock_ReturnsFalse()
    {
        var child = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 3
        };
        await _repository.InsertAsync(child);
        var bundle = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts };
        bundle.BundleProducts.Add(new BundleProduct { ProductId = child.Id, Quantity = 2 });
        await _repository.InsertAsync(bundle);

        Assert.IsFalse(await _inventoryManageService.TryReserve(bundle, 2, null, ""));
        Assert.AreEqual(0, Stored(child).ReservedQuantity);
    }

    [TestMethod]
    public async Task TryReserve_DoesNotWaitOnItsOwnLock()
    {
        var child = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 5
        };
        await _repository.InsertAsync(child);
        var bundle = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts };
        bundle.BundleProducts.Add(new BundleProduct { ProductId = child.Id, Quantity = 2 });
        await _repository.InsertAsync(bundle);
        var stopwatch = Stopwatch.StartNew();

        Assert.IsTrue(await _inventoryManageService.TryReserve(bundle, 1, null, ""));

        Assert.IsLessThan(2000, stopwatch.ElapsedMilliseconds);
        Assert.AreEqual(2, Stored(child).ReservedQuantity);
    }

    [TestMethod]
    public async Task CancelReservation_ReservationUnpublishedProduct_PublishesItAgain()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 5,
            MinStockQuantity = 2,
            LowStockActivityId = LowStockActivity.Unpublish,
            Published = true
        };
        await _repository.InsertAsync(product);
        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 3, null, ""));
        Assert.IsFalse(Stored(product).Published);

        await _inventoryManageService.CancelReservation(product, 3, null, "");

        var stored = Stored(product);
        Assert.IsTrue(stored.Published);
        Assert.AreEqual(0, stored.ReservedQuantity);
    }

    [TestMethod]
    public async Task CancelReservation_StillAtMinimum_StaysUnpublished()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.NoBackorders,
            StockQuantity = 5,
            ReservedQuantity = 3,
            MinStockQuantity = 2,
            LowStockActivityId = LowStockActivity.Unpublish,
            Published = false
        };
        await _repository.InsertAsync(product);
        Assert.IsTrue(await _inventoryManageService.TryReserve(product, 1, null, ""));

        await _inventoryManageService.CancelReservation(product, 1, null, "");

        Assert.IsFalse(Stored(product).Published);
    }

    [TestMethod]
    public async Task AdjustReserved_ParallelReleases_AllApplied()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 10,
            ReservedQuantity = 10
        };
        await _repository.InsertAsync(product);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            _inventoryManageService.AdjustReserved(product, 1))));

        Assert.AreEqual(0, Stored(product).ReservedQuantity);
    }


    [TestMethod]
    public async Task AdjustReservedTest_ManageStock_Reserve()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            ReservedQuantity = 5
        };
        await _repository.InsertAsync(product);
        //Act
        await _inventoryManageService.AdjustReserved(product, -10);
        product = _repository.Table.FirstOrDefault(x => x.Id == product.Id);
        //Assert
        Assert.AreEqual(15, product.ReservedQuantity);
    }

    [TestMethod]
    public async Task AdjustReserved_LowStockUnpublishes_ClearsProductLists()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 5,
            MinStockQuantity = 2,
            LowStockActivityId = LowStockActivity.Unpublish,
            Published = true
        };
        await _repository.InsertAsync(product);

        await _inventoryManageService.AdjustReserved(product, -4);

        Assert.IsFalse(_repository.Table.First(x => x.Id == product.Id).Published);
        _mediatorMock.Verify(m => m.Publish(It.Is<ProductChangedEvent>(e => e.ProductId == product.Id && e.ListsChanged),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AdjustReservedTest_ManageStock_UnblockReserved()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            ReservedQuantity = 10
        };
        await _repository.InsertAsync(product);
        //Act
        await _inventoryManageService.AdjustReserved(product, 8);
        product = _repository.Table.FirstOrDefault(x => x.Id == product.Id);
        //Assert
        Assert.AreEqual(2, product.ReservedQuantity);
    }

    [TestMethod]
    public async Task BookReservedInventoryTest_ManageStock()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 10,
            ReservedQuantity = 10
        };
        await _repository.InsertAsync(product);

        var shipment = new Shipment();
        var shipmentItem = new ShipmentItem { Quantity = 10 };

        //Act
        await _inventoryManageService.BookReservedInventory(product, shipment, shipmentItem);
        product = _repository.Table.FirstOrDefault(x => x.Id == product.Id);
        //Assert
        Assert.AreEqual(0, product.ReservedQuantity);
        Assert.AreEqual(0, product.StockQuantity);
    }

    [TestMethod]
    public async Task ReverseBookedInventoryTest()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 10,
            ReservedQuantity = 10
        };
        await _repository.InsertAsync(product);

        var shipment = new Shipment();
        var shipmentItem = new ShipmentItem { Quantity = 10 };
        await _inventoryManageService.BookReservedInventory(product, shipment, shipmentItem);

        //Act
        await _inventoryManageService.ReverseBookedInventory(shipment, shipmentItem);

        product = _repository.Table.FirstOrDefault(x => x.Id == product.Id);

        //Assert
        Assert.AreEqual(10, product.ReservedQuantity);
        Assert.AreEqual(10, product.StockQuantity);
    }

    [TestMethod]
    public async Task UpdateStockProductTest()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 0,
            ReservedQuantity = 0
        };
        await _repository.InsertAsync(product);

        //Act
        product.StockQuantity = 10;
        product.ReservedQuantity = 10;
        await _inventoryManageService.UpdateStockProduct(product);

        product = _repository.Table.FirstOrDefault(x => x.Id == product.Id);

        //Assert
        Assert.AreEqual(10, product.ReservedQuantity);
        Assert.AreEqual(10, product.StockQuantity);
    }
}