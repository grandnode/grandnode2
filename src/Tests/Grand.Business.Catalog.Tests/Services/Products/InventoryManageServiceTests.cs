using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class InventoryManageServiceTests
{
    private MemoryCacheBase _cacheBase;
    private InventoryManageService _inventoryManageService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _repository;
    private IRepository<InventoryJournal> _repositoryInventoryJournal;
    private CatalogSettings _settings;
    private StockQuantityService _stockQuantityService;
    private Mock<ITranslationService> _translationService;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        CommonPath.BaseDirectory = "";

        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<Product>();
        _repositoryInventoryJournal = new MongoDBRepositoryTest<InventoryJournal>();
        _workContextMock = new Mock<IWorkContext>();
        _translationService = new Mock<ITranslationService>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _settings = new CatalogSettings();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _stockQuantityService = new StockQuantityService();
        _inventoryManageService = new InventoryManageService(_repository, _repositoryInventoryJournal,
            _stockQuantityService, _cacheBase, _mediatorMock.Object, _settings);
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