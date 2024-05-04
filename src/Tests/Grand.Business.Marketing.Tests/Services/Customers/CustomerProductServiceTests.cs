using Grand.Business.Marketing.Services.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Customers;

[TestClass]
public class CustomerProductServiceTests
{
    private MemoryCacheBase _cacheBase;
    private CustomerProductService _customerProductService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CustomerProduct> _repositoryCustomerProduct;
    private IRepository<CustomerProductPrice> _repositoryCustomerProductPrice;

    [TestInitialize]
    public void Init()
    {
        _repositoryCustomerProductPrice = new MongoDBRepositoryTest<CustomerProductPrice>();
        _repositoryCustomerProduct = new MongoDBRepositoryTest<CustomerProduct>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _customerProductService = new CustomerProductService(_repositoryCustomerProductPrice,
            _repositoryCustomerProduct, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetCustomerProductPriceByIdTest()
    {
        //Arrange
        var customerProductPrice = new CustomerProductPrice { Price = 10 };

        await _repositoryCustomerProductPrice.InsertAsync(customerProductPrice);

        //Act
        var result = await _customerProductService.GetCustomerProductPriceById(customerProductPrice.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.Price);
    }

    [TestMethod]
    public async Task GetPriceByCustomerProductTest()
    {
        //Arrange
        var customerProductPrice = new CustomerProductPrice { Price = 10, CustomerId = "1", ProductId = "1" };

        await _repositoryCustomerProductPrice.InsertAsync(customerProductPrice);

        //Act
        var result = await _customerProductService.GetPriceByCustomerProduct("1", "1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public async Task InsertCustomerProductPriceTest()
    {
        //Act
        var customerProductPrice = new CustomerProductPrice { Price = 10, CustomerId = "1", ProductId = "1" };
        await _customerProductService.InsertCustomerProductPrice(customerProductPrice);

        //Assert
        Assert.IsTrue(_repositoryCustomerProductPrice.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCustomerProductPriceTest()
    {
        //Arrange
        var customerProductPrice = new CustomerProductPrice { Price = 10, CustomerId = "1", ProductId = "1" };

        await _repositoryCustomerProductPrice.InsertAsync(customerProductPrice);

        //Act
        customerProductPrice.Price = 20;
        await _customerProductService.UpdateCustomerProductPrice(customerProductPrice);
        var result = await _customerProductService.GetCustomerProductPriceById(customerProductPrice.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(20, result.Price);
    }

    [TestMethod]
    public async Task DeleteCustomerProductPriceTest()
    {
        //Arrange
        var customerProductPrice = new CustomerProductPrice { Price = 10, CustomerId = "1", ProductId = "1" };

        await _repositoryCustomerProductPrice.InsertAsync(customerProductPrice);

        //Act
        await _customerProductService.DeleteCustomerProductPrice(customerProductPrice);
        var result = await _customerProductService.GetCustomerProductPriceById(customerProductPrice.Id);

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetProductsPriceByCustomerTest()
    {
        //Arrange
        await _repositoryCustomerProductPrice.InsertAsync(new CustomerProductPrice { CustomerId = "1" });
        await _repositoryCustomerProductPrice.InsertAsync(new CustomerProductPrice { CustomerId = "1" });
        await _repositoryCustomerProductPrice.InsertAsync(new CustomerProductPrice { CustomerId = "1" });

        //Act
        var result = await _customerProductService.GetProductsPriceByCustomer("1");

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetCustomerProductTest()
    {
        //Arrange
        var customerProduct = new CustomerProduct { CustomerId = "1" };

        await _repositoryCustomerProduct.InsertAsync(customerProduct);

        //Act
        var result = await _customerProductService.GetCustomerProduct(customerProduct.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.CustomerId);
    }

    [TestMethod]
    public async Task GetCustomerProductTest_Customer_Product()
    {
        //Arrange
        var customerProduct = new CustomerProduct { CustomerId = "1", ProductId = "1" };

        await _repositoryCustomerProduct.InsertAsync(customerProduct);

        //Act
        var result = await _customerProductService.GetCustomerProduct("1", "1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.CustomerId);
    }

    [TestMethod]
    public async Task InsertCustomerProductTest()
    {
        //Act
        var customerProduct = new CustomerProduct { CustomerId = "1", ProductId = "1" };
        await _customerProductService.InsertCustomerProduct(customerProduct);

        //Assert
        Assert.IsTrue(_repositoryCustomerProduct.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCustomerProductTest()
    {
        //Arrange
        var customerProduct = new CustomerProduct { CustomerId = "1", ProductId = "1", DisplayOrder = 1 };

        await _repositoryCustomerProduct.InsertAsync(customerProduct);

        //Act
        customerProduct.DisplayOrder = 20;
        await _customerProductService.UpdateCustomerProduct(customerProduct);
        var result = await _customerProductService.GetCustomerProduct(customerProduct.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(20, result.DisplayOrder);
    }

    [TestMethod]
    public async Task DeleteCustomerProductTest()
    {
        //Arrange
        var customerProduct = new CustomerProduct { CustomerId = "1", ProductId = "1" };

        await _repositoryCustomerProduct.InsertAsync(customerProduct);

        //Act
        await _customerProductService.DeleteCustomerProduct(customerProduct);
        var result = await _customerProductService.GetCustomerProduct(customerProduct.Id);

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetProductsByCustomerTest()
    {
        //Arrange
        await _repositoryCustomerProduct.InsertAsync(new CustomerProduct { CustomerId = "1" });
        await _repositoryCustomerProduct.InsertAsync(new CustomerProduct { CustomerId = "1" });
        await _repositoryCustomerProduct.InsertAsync(new CustomerProduct { CustomerId = "1" });

        //Act
        var result = await _customerProductService.GetProductsByCustomer("1");

        //Assert
        Assert.AreEqual(3, result.Count);
    }
}