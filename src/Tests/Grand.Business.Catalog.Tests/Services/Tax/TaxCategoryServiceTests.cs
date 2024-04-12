using Grand.Business.Catalog.Services.Tax;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Tax;

[TestClass]
public class TaxCategoryServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<TaxCategory> _repository;
    private TaxCategoryService _taxCategoryService;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<TaxCategory>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _taxCategoryService = new TaxCategoryService(_cacheBase, _repository, _mediatorMock.Object);
    }


    [TestMethod]
    public async Task GetAllTaxCategoriesTest()
    {
        //Arrange
        await _taxCategoryService.InsertTaxCategory(new TaxCategory());
        await _taxCategoryService.InsertTaxCategory(new TaxCategory());
        await _taxCategoryService.InsertTaxCategory(new TaxCategory());

        //Act
        var result = await _taxCategoryService.GetAllTaxCategories();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetTaxCategoryByIdTest()
    {
        //Arrange
        var taxCategory = new TaxCategory {
            Name = "test1"
        };
        await _repository.InsertAsync(taxCategory);
        //Assert
        var result = await _taxCategoryService.GetTaxCategoryById(taxCategory.Id);

        //Act
        Assert.IsNotNull(result);
        Assert.AreEqual("test1", result.Name);
    }

    [TestMethod]
    public async Task InsertTaxCategoryTest()
    {
        //Act
        await _taxCategoryService.InsertTaxCategory(new TaxCategory());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateTaxCategoryTest()
    {
        //Arrange
        var tax = new TaxCategory {
            Name = "test"
        };
        await _taxCategoryService.InsertTaxCategory(tax);
        tax.Name = "test2";

        //Act
        await _taxCategoryService.UpdateTaxCategory(tax);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task DeleteTaxCategoryTest()
    {
        //Arrange
        var tax = new TaxCategory {
            Name = "test"
        };
        await _taxCategoryService.InsertTaxCategory(tax);
        await _taxCategoryService.InsertTaxCategory(new TaxCategory());
        //Act
        await _taxCategoryService.DeleteTaxCategory(tax);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        Assert.AreEqual(1, _repository.Table.Count());
    }
}