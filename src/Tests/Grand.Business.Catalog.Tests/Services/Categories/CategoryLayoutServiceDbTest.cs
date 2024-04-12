using Grand.Business.Catalog.Services.Categories;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Categories;

[TestClass]
public class CategoryLayoutServiceDbTest
{
    private MemoryCacheBase _cacheBase;
    private CategoryLayoutService _categoryLayoutService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CategoryLayout> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<CategoryLayout>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryLayoutService = new CategoryLayoutService(_repository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetCategoryLayoutById()
    {
        //Arrange
        var categoryLayout1 = new CategoryLayout {
            Name = "test1"
        };
        await _categoryLayoutService.InsertCategoryLayout(categoryLayout1);

        //Assert
        var layout = await _categoryLayoutService.GetCategoryLayoutById(categoryLayout1.Id);

        //Act
        Assert.IsNotNull(layout);
        Assert.AreEqual("test1", layout.Name);
    }

    [TestMethod]
    public async Task GetAllCategoryLayouts()
    {
        //Arrange
        await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
        await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
        await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());

        //Act
        var layouts = await _categoryLayoutService.GetAllCategoryLayouts();

        //Assert
        Assert.AreEqual(3, layouts.Count);
    }

    [TestMethod]
    public async Task DeleteCategoryLayout()
    {
        //Arrange
        var categoryLayout1 = new CategoryLayout {
            Name = "test1"
        };
        await _categoryLayoutService.InsertCategoryLayout(categoryLayout1);
        var categoryLayout2 = new CategoryLayout {
            Name = "test2"
        };
        await _categoryLayoutService.InsertCategoryLayout(categoryLayout2);

        //Act
        await _categoryLayoutService.DeleteCategoryLayout(categoryLayout1);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test1"));
        Assert.AreEqual(1, _repository.Table.Count());
    }


    [TestMethod]
    public async Task InsertCategoryLayout_True()
    {
        //Act
        await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCategoryLayout_IsNotNull()
    {
        //Arrange
        var categoryLayout = new CategoryLayout {
            Name = "test"
        };
        await _categoryLayoutService.InsertCategoryLayout(categoryLayout);
        categoryLayout.Name = "test2";

        //Act
        await _categoryLayoutService.UpdateCategoryLayout(categoryLayout);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task UpdateCategoryLayout_IsNull()
    {
        //Arrange
        var categoryLayout = new CategoryLayout {
            Name = "test"
        };
        await _categoryLayoutService.InsertCategoryLayout(categoryLayout);
        categoryLayout.Name = "test2";

        //Act
        await _categoryLayoutService.UpdateCategoryLayout(categoryLayout);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test3"));
    }
}