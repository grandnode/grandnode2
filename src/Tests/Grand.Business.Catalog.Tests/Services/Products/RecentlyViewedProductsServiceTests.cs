using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class RecentlyViewedProductsServiceTests
{
    private Mock<IProductService> _productServiceMock;
    private IRepository<RecentlyViewedProduct> _repository;
    private RecentlyViewedProductsService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<RecentlyViewedProduct>();
        _productServiceMock = new Mock<IProductService>();
        _productServiceMock.Setup(a => a.GetProductsByIds(It.IsAny<string[]>(),
            It.IsAny<CatalogAccess>())).Returns(() =>
            Task.FromResult((IList<Product>)new List<Product> {
                new() { Id = "1", Published = true }, new() { Id = "2", Published = true },
                new() { Id = "3", Published = true }
            }));
        _service = new RecentlyViewedProductsService(_productServiceMock.Object,
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
    public async Task GetRecentlyViewedProducts_AfterAnotherView_ReadsTheNewList()
    {
        //Arrange
        await _service.AddProductToRecentlyViewedList("1", "1");
        await _service.GetRecentlyViewedProducts(new Customer { Id = "1" }, "", 10);
        await _service.AddProductToRecentlyViewedList("1", "2");

        //Act
        await _service.GetRecentlyViewedProducts(new Customer { Id = "1" }, "", 10);

        //Assert
        _productServiceMock.Verify(a => a.GetProductsByIds(
            It.Is<string[]>(ids => ids.Length == 2 && ids.Contains("1") && ids.Contains("2")),
            It.IsAny<CatalogAccess>()), Times.Once);
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
        await SetViewedAt("1", "1", DateTime.UtcNow.AddMinutes(-2));
        await SetViewedAt("1", "2", DateTime.UtcNow.AddMinutes(-1));

        //Act
        await service.AddProductToRecentlyViewedList("1", "1");
        await service.AddProductToRecentlyViewedList("1", "3");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "1", "3" },
            _repository.Table.Where(x => x.CustomerId == "1").Select(x => x.ProductId).ToList());
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_ProductViewedAgain_KeepsOneRow()
    {
        //Arrange
        await _service.AddProductToRecentlyViewedList("1", "1");

        //Act
        await _service.AddProductToRecentlyViewedList("1", "1");

        //Assert
        Assert.AreEqual(1, _repository.Table.Count(x => x.CustomerId == "1" && x.ProductId == "1"));
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_DuplicateRows_CollapseToOneRowOnTheTop()
    {
        //Arrange
        await _repository.InsertAsync(new RecentlyViewedProduct { CustomerId = "1", ProductId = "1" });
        await _repository.InsertAsync(new RecentlyViewedProduct { CustomerId = "1", ProductId = "1" });
        await _repository.InsertAsync(new RecentlyViewedProduct { CustomerId = "1", ProductId = "2" });
        await SetViewedAt("1", "1", DateTime.UtcNow.AddMinutes(-2));
        await SetViewedAt("1", "2", DateTime.UtcNow.AddMinutes(-1));

        //Act
        await _service.AddProductToRecentlyViewedList("1", "1");

        //Assert
        var other = _repository.Table.Single(x => x.CustomerId == "1" && x.ProductId == "2").CreatedOnUtc;
        var viewedAgain = _repository.Table.Where(x => x.CustomerId == "1" && x.ProductId == "1").ToList();
        Assert.HasCount(1, viewedAgain);
        Assert.IsTrue(viewedAgain[0].CreatedOnUtc > other);
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_LimitLowered_TrimsToTheLimit()
    {
        //Arrange
        var service = CreateService(5);
        for (var i = 1; i <= 5; i++)
            await service.AddProductToRecentlyViewedList("1", i.ToString());

        //Act
        await CreateService(2).AddProductToRecentlyViewedList("1", "6");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "5", "6" },
            _repository.Table.Where(x => x.CustomerId == "1").Select(x => x.ProductId).ToList());
    }

    [TestMethod]
    public async Task AddProductToRecentlyViewedList_ListOverTheLimit_ProductViewedAgain_TrimsToTheLimit()
    {
        //Arrange
        var service = CreateService(5);
        for (var i = 1; i <= 5; i++)
            await service.AddProductToRecentlyViewedList("1", i.ToString());

        //Act
        await CreateService(2).AddProductToRecentlyViewedList("1", "3");

        //Assert
        CollectionAssert.AreEquivalent(new[] { "3", "5" },
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

    private Task SetViewedAt(string customerId, string productId, DateTime viewedOnUtc)
    {
        return _repository.UpdateManyAsync(x => x.CustomerId == customerId && x.ProductId == productId,
            UpdateBuilder<RecentlyViewedProduct>.Create().Set(x => x.CreatedOnUtc, viewedOnUtc));
    }

    private RecentlyViewedProductsService CreateService(int recentlyViewedProductsNumber)
    {
        return new RecentlyViewedProductsService(_productServiceMock.Object,
            new CatalogSettings {
                RecentlyViewedProductsEnabled = true, RecentlyViewedProductsNumber = recentlyViewedProductsNumber
            },
            _repository);
    }
}