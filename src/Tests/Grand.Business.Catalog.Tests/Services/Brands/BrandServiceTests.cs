using Grand.Business.Catalog.Services.Brands;
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

namespace Grand.Business.Catalog.Tests.Services.Brands;

[TestClass]
public class BrandServiceTests
{
    private BrandService _brandService;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Brand> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<Brand>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _brandService = new BrandService(_cacheBase, _repository, _workContextMock.Object, _mediatorMock.Object,
            new AccessControlConfig());
    }


    [TestMethod]
    public async Task GetAllBrandsTest()
    {
        //Arrange
        await _brandService.InsertBrand(new Brand { Published = true });
        await _brandService.InsertBrand(new Brand { Published = true });
        await _brandService.InsertBrand(new Brand { Published = true });

        //Act
        var brand = await _brandService.GetAllBrands();

        //Assert
        Assert.AreEqual(3, brand.Count);
    }

    [TestMethod]
    public async Task GetBrandByIdTest()
    {
        //Arrange
        var brand = new Brand {
            Name = "test"
        };
        await _brandService.InsertBrand(brand);

        //Act
        var layout = await _brandService.GetBrandById(brand.Id);

        //Assert
        Assert.IsNotNull(layout);
        Assert.AreEqual("test", layout.Name);
    }

    [TestMethod]
    public async Task InsertBrandTest()
    {
        //Act
        await _brandService.InsertBrand(new Brand());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateBrandTest()
    {
        //Arrange
        var brand = new Brand {
            Name = "test"
        };
        await _brandService.InsertBrand(brand);
        brand.Name = "test2";

        //Act
        await _brandService.UpdateBrand(brand);

        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
    }

    [TestMethod]
    public async Task DeleteBrandTest()
    {
        //Arrange
        var brand1 = new Brand {
            Name = "test1"
        };
        await _brandService.InsertBrand(brand1);
        var brand2 = new Brand {
            Name = "test2"
        };
        await _brandService.InsertBrand(brand2);

        //Act
        await _brandService.DeleteBrand(brand1);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test1"));
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task GetAllBrandsByDiscountTest()
    {
        //Arrange
        var brand1 = new Brand {
            Name = "test1"
        };
        await _brandService.InsertBrand(brand1);
        var brand2 = new Brand {
            Name = "test2"
        };
        brand2.AppliedDiscounts.Add("disc1");

        await _brandService.InsertBrand(brand2);

        //Act
        var result = await _brandService.GetAllBrandsByDiscount("disc1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }
}