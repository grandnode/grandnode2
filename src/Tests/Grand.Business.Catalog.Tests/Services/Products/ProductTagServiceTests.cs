using Grand.Business.Catalog.Services.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductTagServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _productRepository;
    private ProductTagService _productTagService;
    private IRepository<ProductTag> _repositoryProductTag;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _repositoryProductTag = new MongoDBRepositoryTest<ProductTag>();
        _productRepository = new MongoDBRepositoryTest<Product>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productTagService =
            new ProductTagService(_repositoryProductTag, _productRepository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllProductTagsTest()
    {
        //Arrange
        await _repositoryProductTag.InsertAsync(new ProductTag());
        await _repositoryProductTag.InsertAsync(new ProductTag());
        await _repositoryProductTag.InsertAsync(new ProductTag());

        //Act
        var result = await _productTagService.GetAllProductTags();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetProductTagByIdTest()
    {
        //Arrange
        await _repositoryProductTag.InsertAsync(new ProductTag());
        await _repositoryProductTag.InsertAsync(new ProductTag { Id = "1", Name = "test" });
        await _repositoryProductTag.InsertAsync(new ProductTag());

        //Act
        var result = await _productTagService.GetProductTagById("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetProductTagByNameTest()
    {
        //Arrange
        await _repositoryProductTag.InsertAsync(new ProductTag());
        await _repositoryProductTag.InsertAsync(new ProductTag { Id = "1", Name = "test" });
        await _repositoryProductTag.InsertAsync(new ProductTag());

        //Act
        var result = await _productTagService.GetProductTagByName("test");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.Id);
    }

    [TestMethod]
    public async Task GetProductTagBySeNameTest()
    {
        //Arrange
        await _repositoryProductTag.InsertAsync(new ProductTag());
        await _repositoryProductTag.InsertAsync(new ProductTag { Id = "1", Name = "test", SeName = "test" });
        await _repositoryProductTag.InsertAsync(new ProductTag());

        //Act
        var result = await _productTagService.GetProductTagBySeName("test");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.Id);
    }

    [TestMethod]
    public async Task InsertProductTagTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test"
        };
        //Act
        await _productTagService.InsertProductTag(productTag);
        var result = await _productTagService.GetProductTagById(productTag.Id);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task UpdateProductTagTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test"
        };
        await _productTagService.InsertProductTag(productTag);
        //Act
        productTag.Name = "test2";
        await _productTagService.UpdateProductTag(productTag);
        var result = await _productTagService.GetProductTagById(productTag.Id);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test2", result.Name);
    }

    [TestMethod]
    public async Task DeleteProductTagTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test"
        };
        await _productTagService.InsertProductTag(productTag);

        //Act
        await _productTagService.DeleteProductTag(productTag);
        var result = await _productTagService.GetProductTagById(productTag.Id);

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task AttachProductTagTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test"
        };
        await _productTagService.InsertProductTag(productTag);
        var product = new Product();
        await _productRepository.InsertAsync(product);

        //Act
        await _productTagService.AttachProductTag(productTag, product.Id);
        var result = _productRepository.Table.FirstOrDefault(x => x.Id == product.Id);

        //Assert
        Assert.IsTrue(result.ProductTags.Any());
        Assert.IsNotNull(result.ProductTags.FirstOrDefault(x => x == "test"));
    }

    [TestMethod]
    public async Task DetachProductTagTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test"
        };
        await _productTagService.InsertProductTag(productTag);
        var product = new Product();
        await _productRepository.InsertAsync(product);
        await _productTagService.AttachProductTag(productTag, product.Id);

        //Act
        await _productTagService.DetachProductTag(productTag, product.Id);
        var result = _productRepository.Table.FirstOrDefault(x => x.Id == product.Id);

        //Assert
        Assert.IsFalse(result.ProductTags.Any());
        Assert.IsNull(result.ProductTags.FirstOrDefault(x => x == "test"));
    }

    [TestMethod]
    public async Task GetProductCountTest()
    {
        //Arrange
        var productTag = new ProductTag {
            Name = "test",
            Count = 10
        };
        await _productTagService.InsertProductTag(productTag);
        //Act
        var result = await _productTagService.GetProductCount(productTag.Id);

        //Assert
        Assert.AreEqual(10, result);
    }
}