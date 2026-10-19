using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class RecentlyViewedProductsServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private Mock<IProductService> _productServiceMock;
    private IRepository<RecentlyViewedProduct> _repository;
    private RecentlyViewedProductsService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<RecentlyViewedProduct>();
        _mediatorMock = new Mock<IMediator>();
        _productServiceMock = new Mock<IProductService>();
        _productServiceMock.Setup(a => a.GetProductsByIds(It.IsAny<string[]>(),
            It.IsAny<CatalogAccess>())).Returns(() =>
            Task.FromResult((IList<Product>)new List<Product> {
                new() { Id = "1", Published = true }, new() { Id = "2", Published = true },
                new() { Id = "3", Published = true }
            }));
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _service = new RecentlyViewedProductsService(_productServiceMock.Object, _cacheBase,
            new CatalogSettings { RecentlyViewedProductsEnabled = true, RecentlyViewedProductsNumber = 10 },
            _repository);
    }


    [TestMethod]
    public async Task GetRecentlyViewedProductsTest()
    {
        //Arrange
        await _service.AddProductToRecentlyViewedList("1", "1");
        await _service.AddProductToRecentlyViewedList("1", "2");
        await _service.AddProductToRecentlyViewedList("1", "3");
        await _service.AddProductToRecentlyViewedList("2", "1");

        //Act
        var result = await _service.GetRecentlyViewedProducts(new Customer { Id = "1" }, "", 10);

        //Assert
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedListTest()
    {
        //Act
        await _service.AddProductToRecentlyViewedList("1", "1");
        await _service.AddProductToRecentlyViewedList("1", "2");
        await _service.AddProductToRecentlyViewedList("1", "3");
        await _service.AddProductToRecentlyViewedList("2", "1");


        //Assert
        Assert.AreEqual(4, _repository.Table.Count());
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_KeepsOnlyTheNewestProducts()
    {
        //Arrange
        var service = CreateService(2);

        //Act
        await service.AddProductToRecentlyViewedList("1", "1");
        await service.AddProductToRecentlyViewedList("1", "2");
        await service.AddProductToRecentlyViewedList("1", "3");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "2", "3" },
            _repository.Table.Where(x => x.CustomerId == "1").Select(x => x.ProductId).ToList());
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_ProductViewedAgain_MovesToTheTop()
    {
        //Arrange
        var service = CreateService(2);
        await service.AddProductToRecentlyViewedList("1", "1");
        await service.AddProductToRecentlyViewedList("1", "2");

        //Act
        await service.AddProductToRecentlyViewedList("1", "1");
        await service.AddProductToRecentlyViewedList("1", "3");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "1", "3" },
            _repository.Table.Where(x => x.CustomerId == "1").Select(x => x.ProductId).ToList());
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_NoLimitSet_KeepsTenProducts()
    {
        //Arrange
        var service = CreateService(0);

        //Act
        for (var i = 1; i <= 12; i++)
            await service.AddProductToRecentlyViewedList("1", i.ToString());

        //Assert
        Assert.AreEqual(10, _repository.Table.Count(x => x.CustomerId == "1"));
    }

    private RecentlyViewedProductsService CreateService(int recentlyViewedProductsNumber)
    {
        return new RecentlyViewedProductsService(_productServiceMock.Object, _cacheBase,
            new CatalogSettings {
                RecentlyViewedProductsEnabled = true, RecentlyViewedProductsNumber = recentlyViewedProductsNumber
            },
            _repository);
    }
}