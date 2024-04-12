using Grand.Business.Catalog.Services.Collections;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Collections;

[TestClass]
public class ProductCollectionServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private ProductCollectionService _productCollectionService;
    private IRepository<Product> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<Product>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productCollectionService = new ProductCollectionService(_cacheBase, _repository, _workContextMock.Object,
            _mediatorMock.Object, new AccessControlConfig());
    }


    [TestMethod]
    public async Task GetProductCollectionsByCollectionIdTest()
    {
        //Arrange
        var p1 = new Product();
        p1.ProductCollections.Add(new ProductCollection { CollectionId = "1" });
        p1.ProductCollections.Add(new ProductCollection { CollectionId = "2" });
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        p2.ProductCollections.Add(new ProductCollection { CollectionId = "2" });
        p2.ProductCollections.Add(new ProductCollection { CollectionId = "3" });
        await _repository.InsertAsync(p2);

        //Act
        var pc1 = await _productCollectionService.GetProductCollectionsByCollectionId("1", "");
        var pc2 = await _productCollectionService.GetProductCollectionsByCollectionId("2", "");

        //Assert
        Assert.AreEqual(1, pc1.Count);
        Assert.AreEqual(2, pc2.Count);
    }

    [TestMethod]
    public async Task InsertProductCollectionTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        await _productCollectionService.InsertProductCollection(
            new ProductCollection { CollectionId = "1", DisplayOrder = 10 }, p1.Id);
        await _productCollectionService.InsertProductCollection(new ProductCollection { CollectionId = "2" }, p1.Id);

        var pc1 = await _productCollectionService.GetProductCollectionsByCollectionId("1", "");

        //Assert
        Assert.AreEqual(1, pc1.Count);
        Assert.AreEqual(10, pc1.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task UpdateProductCollectionTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        var pc = new ProductCollection { CollectionId = "1", DisplayOrder = 10 };
        await _productCollectionService.InsertProductCollection(pc, p1.Id);
        await _productCollectionService.InsertProductCollection(new ProductCollection { CollectionId = "2" }, p1.Id);

        pc.CollectionId = "10";
        pc.DisplayOrder = 5;
        await _productCollectionService.UpdateProductCollection(pc, p1.Id);

        var pc1 = await _productCollectionService.GetProductCollectionsByCollectionId("10", "");

        //Assert
        Assert.AreEqual(1, pc1.Count);
        Assert.AreEqual(5, pc1.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task DeleteProductCollectionTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        var pc = new ProductCollection { CollectionId = "1", DisplayOrder = 10 };
        await _productCollectionService.InsertProductCollection(pc, p1.Id);
        await _productCollectionService.InsertProductCollection(new ProductCollection { CollectionId = "2" }, p1.Id);

        await _productCollectionService.DeleteProductCollection(pc, p1.Id);

        var pc1 = await _productCollectionService.GetProductCollectionsByCollectionId("1", "");

        //Assert
        Assert.AreEqual(0, pc1.Count);
    }
}