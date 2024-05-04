using Grand.Business.Catalog.Services.Brands;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Brands;

[TestClass]
public class BrandLayoutServiceTests
{
    private BrandLayoutService _brandLayoutService;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<BrandLayout> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<BrandLayout>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _brandLayoutService = new BrandLayoutService(_repository, _cacheBase, _mediatorMock.Object);
    }


    [TestMethod]
    public async Task GetAllBrandLayoutsTest()
    {
        //Arrange
        await _brandLayoutService.InsertBrandLayout(new BrandLayout());
        await _brandLayoutService.InsertBrandLayout(new BrandLayout());
        await _brandLayoutService.InsertBrandLayout(new BrandLayout());

        //Act
        var layouts = await _brandLayoutService.GetAllBrandLayouts();

        //Assert
        Assert.AreEqual(3, layouts.Count);
    }

    [TestMethod]
    public async Task GetBrandLayoutByIdTest()
    {
        //Arrange
        var brandLayout = new BrandLayout {
            Name = "test"
        };
        await _brandLayoutService.InsertBrandLayout(brandLayout);

        //Act
        var layout = await _brandLayoutService.GetBrandLayoutById(brandLayout.Id);

        //Assert
        Assert.IsNotNull(layout);
        Assert.AreEqual("test", layout.Name);
    }

    [TestMethod]
    public async Task InsertBrandLayoutTest()
    {
        //Act
        await _brandLayoutService.InsertBrandLayout(new BrandLayout());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateBrandLayoutTest()
    {
        //Arrange
        var categoryLayout = new BrandLayout {
            Name = "test"
        };
        await _brandLayoutService.InsertBrandLayout(categoryLayout);
        categoryLayout.Name = "test2";

        //Act
        await _brandLayoutService.UpdateBrandLayout(categoryLayout);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task DeleteBrandLayoutTest()
    {
        //Arrange
        var brandLayout1 = new BrandLayout {
            Name = "test1"
        };
        await _brandLayoutService.InsertBrandLayout(brandLayout1);
        var brandLayout2 = new BrandLayout {
            Name = "test2"
        };
        await _brandLayoutService.InsertBrandLayout(brandLayout2);

        //Act
        await _brandLayoutService.DeleteBrandLayout(brandLayout1);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test1"));
        Assert.AreEqual(1, _repository.Table.Count());
    }
}