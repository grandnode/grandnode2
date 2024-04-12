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
public class ProductAttributeServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private ProductAttributeService _productAttributeService;
    private IRepository<Product> _repository;
    private IRepository<ProductAttribute> _repositoryproductAttribute;

    [TestInitialize]
    public void Init()
    {
        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<Product>();
        _repositoryproductAttribute = new MongoDBRepositoryTest<ProductAttribute>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _productAttributeService =
            new ProductAttributeService(_cacheBase, _repositoryproductAttribute, _repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllProductAttributesTest()
    {
        //Arange
        var pa1 = new ProductAttribute();
        var pa2 = new ProductAttribute();
        var pa3 = new ProductAttribute();
        await _repositoryproductAttribute.InsertAsync(pa1);
        await _repositoryproductAttribute.InsertAsync(pa2);
        await _repositoryproductAttribute.InsertAsync(pa3);

        //Act
        var result = await _productAttributeService.GetAllProductAttributes();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetProductAttributeByIdTest()
    {
        //Arange
        var pa1 = new ProductAttribute();
        var pa2 = new ProductAttribute();
        var pa3 = new ProductAttribute();
        await _repositoryproductAttribute.InsertAsync(pa1);
        await _repositoryproductAttribute.InsertAsync(pa2);
        await _repositoryproductAttribute.InsertAsync(pa3);

        //Act
        var result = await _productAttributeService.GetProductAttributeById(pa1.Id);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertProductAttributeTest()
    {
        //Arange
        var pa1 = new ProductAttribute();

        //Act
        await _productAttributeService.InsertProductAttribute(pa1);

        //Assert
        Assert.IsTrue(_repositoryproductAttribute.Table.Any());
    }

    [TestMethod]
    public async Task UpdateProductAttributeTest()
    {
        //Arange
        var pa1 = new ProductAttribute();
        await _productAttributeService.InsertProductAttribute(pa1);
        pa1.Name = "test";
        //Act
        await _productAttributeService.UpdateProductAttribute(pa1);
        //Assert
        Assert.IsTrue(_repositoryproductAttribute.Table.FirstOrDefault().Name == "test");
    }

    [TestMethod]
    public async Task DeleteProductAttributeTest()
    {
        //Arange
        var pa1 = new ProductAttribute();
        await _productAttributeService.InsertProductAttribute(pa1);

        //Act
        await _productAttributeService.DeleteProductAttribute(pa1);

        //Assert
        Assert.IsFalse(_repositoryproductAttribute.Table.Any());
    }

    [TestMethod]
    public async Task DeleteProductAttributeMappingTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        product.ProductAttributeMappings.Add(pm1);
        var pm2 = new ProductAttributeMapping();
        product.ProductAttributeMappings.Add(pm2);
        var pm3 = new ProductAttributeMapping();
        product.ProductAttributeMappings.Add(pm3);
        _repository.Insert(product);

        //Act
        await _productAttributeService.DeleteProductAttributeMapping(pm1, product.Id);
        //Assert
        Assert.AreEqual(2, _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings.Count);
    }

    [TestMethod]
    public async Task InsertProductAttributeMappingTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        _repository.Insert(product);

        //Act
        await _productAttributeService.InsertProductAttributeMapping(pm1, product.Id);
        //Assert
        Assert.AreEqual(1, _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings.Count);
    }

    [TestMethod]
    public async Task UpdateProductAttributeMappingTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        product.ProductAttributeMappings.Add(pm1);
        _repository.Insert(product);
        //Act
        pm1.TextPrompt = "test";
        await _productAttributeService.UpdateProductAttributeMapping(pm1, product.Id);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
            .FirstOrDefault(x => x.Id == pm1.Id).TextPrompt == "test");
    }

    [TestMethod]
    public async Task DeleteProductAttributeValueTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        var pav1 = new ProductAttributeValue();
        pm1.ProductAttributeValues.Add(pav1);
        var pav2 = new ProductAttributeValue();
        pm1.ProductAttributeValues.Add(pav2);
        product.ProductAttributeMappings.Add(pm1);
        _repository.Insert(product);

        //Act
        await _productAttributeService.DeleteProductAttributeValue(pav1, product.Id, pm1.Id);
        //Assert
        Assert.AreEqual(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
                .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues.Count);
    }

    [TestMethod]
    public async Task InsertProductAttributeValueTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        var pav1 = new ProductAttributeValue();
        product.ProductAttributeMappings.Add(pm1);
        _repository.Insert(product);

        //Act
        await _productAttributeService.InsertProductAttributeValue(pav1, product.Id, pm1.Id);
        //Assert
        Assert.AreEqual(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
                .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues.Count);
    }

    [TestMethod]
    public async Task UpdateProductAttributeValueTest()
    {
        //Arrange
        var product = new Product();
        var pm1 = new ProductAttributeMapping();
        var pav1 = new ProductAttributeValue();
        pm1.ProductAttributeValues.Add(pav1);
        product.ProductAttributeMappings.Add(pm1);
        _repository.Insert(product);

        //Act
        pav1.Name = "test";
        await _productAttributeService.UpdateProductAttributeValue(pav1, product.Id, pm1.Id);

        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
            .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues.FirstOrDefault().Name == "test");
    }


    [TestMethod]
    public async Task DeleteProductAttributeCombinationTest()
    {
        //Arrange
        var product = new Product();
        var pac1 = new ProductAttributeCombination();
        product.ProductAttributeCombinations.Add(pac1);
        var pac2 = new ProductAttributeCombination();
        product.ProductAttributeCombinations.Add(pac2);
        var pac3 = new ProductAttributeCombination();
        product.ProductAttributeCombinations.Add(pac3);
        _repository.Insert(product);

        //Act
        await _productAttributeService.DeleteProductAttributeCombination(pac1, product.Id);
        //Assert
        Assert.AreEqual(2,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations.Count);
    }

    [TestMethod]
    public async Task InsertProductAttributeCombinationTest()
    {
        //Arrange
        var product = new Product();
        _repository.Insert(product);
        var pac1 = new ProductAttributeCombination();
        //Act
        await _productAttributeService.InsertProductAttributeCombination(pac1, product.Id);
        //Assert
        Assert.AreEqual(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations.Count);
    }

    [TestMethod]
    public async Task UpdateProductAttributeCombinationTest()
    {
        //Arrange
        var product = new Product();
        var pac1 = new ProductAttributeCombination();
        product.ProductAttributeCombinations.Add(pac1);
        _repository.Insert(product);
        //Act
        pac1.Text = "test";
        await _productAttributeService.UpdateProductAttributeCombination(pac1, product.Id);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations
            .FirstOrDefault(x => x.Id == pac1.Id).Text == "test");
    }
}