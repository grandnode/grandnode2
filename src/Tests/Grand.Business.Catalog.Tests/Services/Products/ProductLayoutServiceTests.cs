using Grand.Business.Catalog.Services.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductLayoutServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private ProductLayoutService _productLayoutService;
    private IRepository<ProductLayout> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ProductLayout>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productLayoutService = new ProductLayoutService(_repository, _cacheBase, _mediatorMock.Object);
    }


    [TestMethod]
    public async Task GetAllProductLayoutsTest()
    {
        //Arrange
        await _productLayoutService.InsertProductLayout(new ProductLayout());
        await _productLayoutService.InsertProductLayout(new ProductLayout());
        await _productLayoutService.InsertProductLayout(new ProductLayout());

        //Act
        var layouts = await _productLayoutService.GetAllProductLayouts();

        //Assert
        Assert.AreEqual(3, layouts.Count);
    }

    [TestMethod]
    public async Task GetProductLayoutByIdTest()
    {
        //Arrange
        var productLayout = new ProductLayout {
            Name = "test"
        };
        await _productLayoutService.InsertProductLayout(productLayout);

        //Act
        var layout = await _productLayoutService.GetProductLayoutById(productLayout.Id);

        //Assert
        Assert.IsNotNull(layout);
        Assert.AreEqual("test", layout.Name);
    }

    [TestMethod]
    public async Task InsertProductLayoutTest()
    {
        //Act
        await _productLayoutService.InsertProductLayout(new ProductLayout());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateProductLayoutTest()
    {
        //Arrange
        var categoryLayout = new ProductLayout {
            Name = "test"
        };
        await _productLayoutService.InsertProductLayout(categoryLayout);
        categoryLayout.Name = "test2";

        //Act
        await _productLayoutService.UpdateProductLayout(categoryLayout);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task DeleteProductLayoutTest()
    {
        //Arrange
        var productLayout1 = new ProductLayout {
            Name = "test1"
        };
        await _productLayoutService.InsertProductLayout(productLayout1);
        var productLayout2 = new ProductLayout {
            Name = "test2"
        };
        await _productLayoutService.InsertProductLayout(productLayout2);

        //Act
        await _productLayoutService.DeleteProductLayout(productLayout1);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test1"));
        Assert.AreEqual(1, _repository.Table.Count());
    }
}