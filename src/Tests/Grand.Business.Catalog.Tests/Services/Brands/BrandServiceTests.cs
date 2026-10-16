using Grand.Business.Catalog.Services.Brands;
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

namespace Grand.Business.Catalog.Tests.Services.Brands;

[TestClass]
public class BrandServiceTests
{
    private BrandService _brandService;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Brand> _repository;

    [TestInitialize]
    public void InitializeTests()
    {
        _repository = new MongoDBRepositoryTest<Brand>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _brandService = new BrandService(_cacheBase, _repository, _mediatorMock.Object,
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
        var brand = await _brandService.GetAllBrands(brandName: "", access: CatalogAccess.For(new Customer(), ""));

        //Assert
        Assert.HasCount(3, brand);
    }

    [TestMethod]
    public async Task GetAllBrands_FiltersByGroupsOfGivenCustomer()
    {
        //Arrange
        await _repository.InsertAsync(new Brand { Id = "open", Name = "open", Published = true });
        await _repository.InsertAsync(new Brand {
            Id = "limited", Name = "limited", Published = true, LimitedToGroups = true,
            CustomerGroups = new List<string> { "vip" }
        });
        var vipCustomer = new Customer();
        vipCustomer.Groups.Add("vip");

        //Act
        var guest = await _brandService.GetAllBrands("", CatalogAccess.For(new Customer(), ""));
        var vip = await _brandService.GetAllBrands("", CatalogAccess.For(vipCustomer, ""));

        //Assert
        Assert.HasCount(1, guest);
        Assert.HasCount(2, vip);
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
        Assert.IsNotEmpty(_repository.Table);
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
        Assert.HasCount(1, _repository.Table);
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
        Assert.HasCount(1, result);
    }
}