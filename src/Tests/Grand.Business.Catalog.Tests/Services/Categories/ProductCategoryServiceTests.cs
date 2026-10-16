using Grand.Business.Catalog.Services.Categories;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Categories;

[TestClass]
public class ProductCategoryServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private ProductCategoryService _productCategoryService;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void InitializeTests()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productCategoryService = new ProductCategoryService(_repository, _cacheBase,
            _mediatorMock.Object, new AccessControlConfig());
    }


    [TestMethod]
    public async Task GetProductCategoriesByCategoryId_FiltersByGivenStore()
    {
        //Arrange
        var product = new Product { LimitedToStores = true, Stores = new List<string> { "store1" } };
        product.ProductCategories.Add(new ProductCategory { CategoryId = "cat" });
        await _repository.InsertAsync(product);

        //Act
        var store1 = await _productCategoryService.GetProductCategoriesByCategoryId("cat", new Customer(), "store1");
        var store2 = await _productCategoryService.GetProductCategoriesByCategoryId("cat", new Customer(), "store2");

        //Assert
        Assert.HasCount(1, store1);
        Assert.IsEmpty(store2);
    }

    [TestMethod]
    public async Task GetProductCategoriesByCategoryId_ShowHiddenWithoutCustomer_ReturnsLimitedProducts()
    {
        //Arrange
        var product = new Product {
            LimitedToStores = true, Stores = new List<string> { "store1" },
            LimitedToGroups = true, CustomerGroups = new List<string> { "vip" }
        };
        product.ProductCategories.Add(new ProductCategory { CategoryId = "cat" });
        await _repository.InsertAsync(product);

        //Act
        var mappings = await _productCategoryService.GetProductCategoriesByCategoryId("cat", customer: null, storeId: "",
            showHidden: true);

        //Assert
        Assert.HasCount(1, mappings);
    }

    [TestMethod]
    public async Task GetProductCategoriesByCategoryIdTest()
    {
        //Arrange
        var p1 = new Product();
        p1.ProductCategories.Add(new ProductCategory { CategoryId = "1" });
        p1.ProductCategories.Add(new ProductCategory { CategoryId = "2" });
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        p2.ProductCategories.Add(new ProductCategory { CategoryId = "2" });
        p2.ProductCategories.Add(new ProductCategory { CategoryId = "3" });
        await _repository.InsertAsync(p2);

        //Act
        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1", new Customer(), "");
        var pc2 = await _productCategoryService.GetProductCategoriesByCategoryId("2", new Customer(), "");

        //Assert
        Assert.HasCount(1, pc1);
        Assert.HasCount(2, pc2);
    }

    [TestMethod]
    public async Task InsertProductCategoryTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        await _productCategoryService.InsertProductCategory(new ProductCategory { CategoryId = "1", DisplayOrder = 10 },
            p1.Id);
        await _productCategoryService.InsertProductCategory(new ProductCategory { CategoryId = "2" }, p1.Id);

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1", new Customer(), "");

        //Assert
        Assert.HasCount(1, pc1);
        Assert.AreEqual(10, pc1.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task UpdateProductCategoryTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        var pc = new ProductCategory { CategoryId = "1", DisplayOrder = 10 };
        await _productCategoryService.InsertProductCategory(pc, p1.Id);
        await _productCategoryService.InsertProductCategory(new ProductCategory { CategoryId = "2" }, p1.Id);

        pc.CategoryId = "10";
        pc.DisplayOrder = 5;
        await _productCategoryService.UpdateProductCategory(pc, p1.Id);

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("10", new Customer(), "");

        //Assert
        Assert.HasCount(1, pc1);
        Assert.AreEqual(5, pc1.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task DeleteProductCategoryTest()
    {
        //Arrange
        var p1 = new Product();
        await _repository.InsertAsync(p1);
        var p2 = new Product();
        await _repository.InsertAsync(p2);

        //Act
        var pc = new ProductCategory { CategoryId = "1", DisplayOrder = 10 };
        await _productCategoryService.InsertProductCategory(pc, p1.Id);
        await _productCategoryService.InsertProductCategory(new ProductCategory { CategoryId = "2" }, p1.Id);

        await _productCategoryService.DeleteProductCategory(pc, p1.Id);

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1", new Customer(), "");

        //Assert
        Assert.IsEmpty(pc1);
    }
}