using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
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
public class SpecificationAttributeServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private Mock<IProductService> _productServiceMock;
    private IRepository<SpecificationAttribute> _repository;
    private IRepository<Product> _repositoryProduct;

    private SpecificationAttributeService service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<SpecificationAttribute>();
        _repositoryProduct = new MongoDBRepositoryTest<Product>();
        _mediatorMock = new Mock<IMediator>();
        _productServiceMock = new Mock<IProductService>();
        _productServiceMock.Setup(a => a.GetProductsByIds(It.IsAny<string[]>(), false)).Returns(() =>
            Task.FromResult((IList<Product>)new List<Product> {
                new() { Id = "1", Published = true }, new() { Id = "2", Published = true },
                new() { Id = "3", Published = true }
            }));
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        service = new SpecificationAttributeService(_cacheBase, _repository, _repositoryProduct, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetSpecificationAttributeByIdTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test"
        };
        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        var result = await service.GetSpecificationAttributeById(specificationAttribute.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetSpecificationAttributeBySeNameTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };
        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        var result = await service.GetSpecificationAttributeBySeName("test");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetSpecificationAttributesTest()
    {
        //Arrange
        await service.InsertSpecificationAttribute(new SpecificationAttribute());
        await service.InsertSpecificationAttribute(new SpecificationAttribute());
        await service.InsertSpecificationAttribute(new SpecificationAttribute());

        //Act
        var result = await service.GetSpecificationAttributes();

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task InsertSpecificationAttributeTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };

        //Act
        await service.InsertSpecificationAttribute(specificationAttribute);

        //Assert
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task UpdateSpecificationAttributeTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };
        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        specificationAttribute.Name = "test2";
        await service.UpdateSpecificationAttribute(specificationAttribute);

        //Assert
        Assert.AreEqual("test2", _repository.Table.FirstOrDefault(x => x.Id == specificationAttribute.Id).Name);
    }

    [TestMethod]
    public async Task DeleteSpecificationAttributeTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };
        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        await service.DeleteSpecificationAttribute(specificationAttribute);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == specificationAttribute.Id));
    }

    [TestMethod]
    public async Task GetSpecificationAttributeByOptionIdTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };
        var attr = new SpecificationAttributeOption();
        specificationAttribute.SpecificationAttributeOptions.Add(attr);

        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        var result = await service.GetSpecificationAttributeByOptionId(attr.Id);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task DeleteSpecificationAttributeOptionTest()
    {
        //Arrange
        var specificationAttribute = new SpecificationAttribute {
            Name = "test",
            SeName = "test"
        };
        var attr = new SpecificationAttributeOption();
        specificationAttribute.SpecificationAttributeOptions.Add(attr);

        await service.InsertSpecificationAttribute(specificationAttribute);

        //Act
        await service.DeleteSpecificationAttributeOption(attr);

        //Assert
        Assert.AreEqual(0,
            _repository.Table.FirstOrDefault(x => x.Id == specificationAttribute.Id).SpecificationAttributeOptions
                .Count);
    }

    [TestMethod]
    public async Task InsertProductSpecificationAttributeTest()
    {
        //Arrange
        var product = new Product();

        await _repositoryProduct.InsertAsync(product);

        //Act
        var attr = new ProductSpecificationAttribute();
        await service.InsertProductSpecificationAttribute(attr, product.Id);

        //Assert
        Assert.AreEqual(1,
            _repositoryProduct.Table.FirstOrDefault(x => x.Id == product.Id).ProductSpecificationAttributes.Count);
    }

    [TestMethod]
    public async Task UpdateProductSpecificationAttributeTest()
    {
        //Arrange
        var product = new Product();
        await _repositoryProduct.InsertAsync(product);
        var attr = new ProductSpecificationAttribute();
        await service.InsertProductSpecificationAttribute(attr, product.Id);

        //Act
        attr.CustomName = "test";
        await service.UpdateProductSpecificationAttribute(attr, product.Id);

        //Assert
        Assert.AreEqual("test",
            _repositoryProduct.Table.FirstOrDefault(x => x.Id == product.Id).ProductSpecificationAttributes
                .FirstOrDefault(x => x.Id == attr.Id).CustomName);
    }

    [TestMethod]
    public async Task DeleteProductSpecificationAttributeTest()
    {
        //Arrange
        var product = new Product();
        await _repositoryProduct.InsertAsync(product);

        var attr = new ProductSpecificationAttribute();
        await service.InsertProductSpecificationAttribute(attr, product.Id);

        //Act
        await service.DeleteProductSpecificationAttribute(attr, product.Id);

        //Assert
        Assert.AreEqual(0,
            _repositoryProduct.Table.FirstOrDefault(x => x.Id == product.Id).ProductSpecificationAttributes.Count);
    }

    [TestMethod]
    public async Task GetProductSpecificationAttributeCountTest()
    {
        //Arrange
        var product = new Product();
        await _repositoryProduct.InsertAsync(product);

        var attr = new ProductSpecificationAttribute();
        await service.InsertProductSpecificationAttribute(attr, product.Id);

        //Act
        var result = service.GetProductSpecificationAttributeCount();

        //Assert
        Assert.AreEqual(1, result);
    }
}