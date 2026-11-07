using Grand.Business.Catalog.Services.Products;
using Grand.Business.Catalog.Tests.Services.Inventory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
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
    private IRepository<InventoryJournal> _journal;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _repositoryproductAttribute = new MongoDBRepositoryTest<ProductAttribute>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _journal = new MongoDBRepositoryTest<InventoryJournal>();

        _productAttributeService =
            new ProductAttributeService(_cacheBase, _repositoryproductAttribute, _repository, _mediatorMock.Object,
                new MongoResourceLockTest(), StockTestFixture.Ledger(_repository, _journal));
    }

    [TestMethod]
    public async Task UpdateProductAttributeCombination_StaleStock_StoredStockKept()
    {
        var combination = new ProductAttributeCombination {
            StockQuantity = 6, ReservedQuantity = 2,
            WarehouseInventory = { new ProductCombinationWarehouseInventory { WarehouseId = "w1", StockQuantity = 6 } }
        };
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            ProductAttributeCombinations = { combination }
        };
        await _repository.InsertAsync(product);

        var edited = (ProductAttributeCombination)combination.Clone();
        edited.WarehouseInventory.Clear();
        edited.StockQuantity = 1;
        edited.ReservedQuantity = 0;
        edited.Sku = "sku";
        await _productAttributeService.UpdateProductAttributeCombination(edited, product.Id);

        var stored = _repository.Table.First(x => x.Id == product.Id).ProductAttributeCombinations.First();
        Assert.AreEqual("sku", stored.Sku);
        Assert.AreEqual(6, stored.StockQuantity);
        Assert.AreEqual(2, stored.ReservedQuantity);
        Assert.AreEqual(6, stored.WarehouseInventory.Single().StockQuantity);
    }

    [TestMethod]
    public async Task UpdateProductAttributeCombination_EmptyRowLeftOut_IsRemovedAndNewRowStartsEmpty()
    {
        var combination = new ProductAttributeCombination {
            WarehouseInventory = {
                new ProductCombinationWarehouseInventory { WarehouseId = "w1" },
                new ProductCombinationWarehouseInventory { WarehouseId = "w2", StockQuantity = 3 }
            }
        };
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes, UseMultipleWarehouses = true,
            ProductAttributeCombinations = { combination }
        };
        await _repository.InsertAsync(product);

        var edited = new ProductAttributeCombination {
            Id = combination.Id,
            WarehouseInventory = { new ProductCombinationWarehouseInventory { WarehouseId = "w3", StockQuantity = 9 } }
        };
        await _productAttributeService.UpdateProductAttributeCombination(edited, product.Id);

        var rows = _repository.Table.First(x => x.Id == product.Id).ProductAttributeCombinations.Single()
            .WarehouseInventory.OrderBy(x => x.WarehouseId).ToList();
        CollectionAssert.AreEqual(new[] { "w2", "w3" }, rows.Select(x => x.WarehouseId).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 0 }, rows.Select(x => x.StockQuantity).ToArray());
    }

    [TestMethod]
    public async Task InsertProductAttributeCombination_WithStock_InsertedWithoutStock()
    {
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes };
        await _repository.InsertAsync(product);

        await _productAttributeService.InsertProductAttributeCombination(
            new ProductAttributeCombination { StockQuantity = 4, ReservedQuantity = 1 }, product.Id);

        var stored = _repository.Table.First(x => x.Id == product.Id).ProductAttributeCombinations.Single();
        Assert.AreEqual(0, stored.StockQuantity);
        Assert.AreEqual(0, stored.ReservedQuantity);
    }

    [TestMethod]
    public async Task DeleteProductAttributeCombination_BooksItsStockOutAndRecomputesProductSum()
    {
        var combination = new ProductAttributeCombination
            { Attributes = StockTestFixture.Attributes("red"), StockQuantity = 6 };
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes, StockQuantity = 9,
            ProductAttributeCombinations = {
                combination,
                new ProductAttributeCombination { Attributes = StockTestFixture.Attributes("blue"), StockQuantity = 3 }
            }
        };
        await _repository.InsertAsync(product);

        await _productAttributeService.DeleteProductAttributeCombination(combination, product.Id);

        Assert.AreEqual(3, _repository.Table.First(x => x.Id == product.Id).StockQuantity);
        var entry = _journal.Table.Single(x => x.ProductId == product.Id);
        Assert.AreEqual(combination.Id, entry.CombinationId);
        Assert.AreEqual(6, entry.OutQty);
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
        Assert.HasCount(3, result);
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
        Assert.AreEqual("test", _repositoryproductAttribute.Table.FirstOrDefault().Name);
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
        Assert.HasCount(2, _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings);
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
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings);
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
        Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
            .FirstOrDefault(x => x.Id == pm1.Id).TextPrompt);
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
        Assert.HasCount(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
                .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues);
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
        Assert.HasCount(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
                .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues);
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
        Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeMappings
            .FirstOrDefault(x => x.Id == pm1.Id).ProductAttributeValues.FirstOrDefault().Name);
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
        Assert.HasCount(2,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations);
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
        Assert.HasCount(1,
            _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations);
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
        Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Id == product.Id).ProductAttributeCombinations
            .FirstOrDefault(x => x.Id == pac1.Id).Text);
    }
}