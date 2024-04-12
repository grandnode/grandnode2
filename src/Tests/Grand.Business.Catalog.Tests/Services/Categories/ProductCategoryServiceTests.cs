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
using Grand.SharedKernel.Extensions;
using MediatR;
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
        _productCategoryService = new ProductCategoryService(_repository, _cacheBase, _workContextMock.Object,
            _mediatorMock.Object, new AccessControlConfig());
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
        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1");
        var pc2 = await _productCategoryService.GetProductCategoriesByCategoryId("2");

        //Assert
        Assert.AreEqual(1, pc1.Count);
        Assert.AreEqual(2, pc2.Count);
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

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1");

        //Assert
        Assert.AreEqual(1, pc1.Count);
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

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("10");

        //Assert
        Assert.AreEqual(1, pc1.Count);
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

        var pc1 = await _productCategoryService.GetProductCategoriesByCategoryId("1");

        //Assert
        Assert.AreEqual(0, pc1.Count);
    }
}