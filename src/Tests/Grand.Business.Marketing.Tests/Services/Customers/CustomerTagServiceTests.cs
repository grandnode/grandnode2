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
public class CustomerTagServiceTests
{
    private MemoryCacheBase _cacheBase;
    private CustomerTagService _customerTagService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Customer> _repositoryCustomer;
    private IRepository<CustomerTag> _repositoryCustomerTag;
    private IRepository<CustomerTagProduct> _repositoryCustomerTagProduct;

    [TestInitialize]
    public void Init()
    {
        _repositoryCustomerTag = new MongoDBRepositoryTest<CustomerTag>();
        _repositoryCustomerTagProduct = new MongoDBRepositoryTest<CustomerTagProduct>();
        _repositoryCustomer = new MongoDBRepositoryTest<Customer>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _customerTagService = new CustomerTagService(_repositoryCustomerTag, _repositoryCustomerTagProduct,
            _repositoryCustomer, _mediatorMock.Object, _cacheBase);
    }

    [TestMethod]
    public async Task GetCustomersByTagTest()
    {
        //Arrange
        var customer1 = new Customer();
        customer1.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer1);
        var customer2 = new Customer();
        customer2.CustomerTags.Add("2");
        await _repositoryCustomer.InsertAsync(customer2);
        var customer3 = new Customer();
        customer3.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer3);

        //Act
        var result = await _customerTagService.GetCustomersByTag("1");

        //Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task DeleteCustomerTagTest()
    {
        //Arrange
        var customer1 = new Customer();
        customer1.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer1);
        var customer2 = new Customer();
        customer2.CustomerTags.Add("2");
        await _repositoryCustomer.InsertAsync(customer2);
        var customer3 = new Customer();
        customer3.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer3);

        var customerTag = new CustomerTag { Id = "1" };
        await _repositoryCustomerTag.InsertAsync(customerTag);

        //Act
        await _customerTagService.DeleteCustomerTag(customerTag);
        var result = await _customerTagService.GetCustomersByTag("1");

        //Assert
        Assert.IsFalse(result.Any());
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, _repositoryCustomerTag.Table.Count());
    }

    [TestMethod]
    public async Task GetAllCustomerTagsTest()
    {
        //Assert
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());

        //Act
        var result = await _customerTagService.GetAllCustomerTags();

        //Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetCustomerTagByIdTest()
    {
        //Assert
        var customerTag = new CustomerTag { Id = "1", Name = "test" };
        await _repositoryCustomerTag.InsertAsync(customerTag);
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());

        //Act
        var result = await _customerTagService.GetCustomerTagById(customerTag.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetCustomerTagByNameTest()
    {
        //Assert
        var customerTag = new CustomerTag { Id = "1", Name = "test" };
        await _repositoryCustomerTag.InsertAsync(customerTag);
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());

        //Act
        var result = await _customerTagService.GetCustomerTagByName("test");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task GetCustomerTagsByNameTest()
    {
        //Assert
        var customerTag = new CustomerTag { Id = "1", Name = "test" };
        await _repositoryCustomerTag.InsertAsync(customerTag);
        await _repositoryCustomerTag.InsertAsync(new CustomerTag { Name = "test" });
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());
        await _repositoryCustomerTag.InsertAsync(new CustomerTag());

        //Act
        var result = await _customerTagService.GetCustomerTagsByName("test");

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task InsertCustomerTagTest()
    {
        //Assert
        var customerTag = new CustomerTag { Id = "1", Name = "test" };

        //Act
        await _customerTagService.InsertCustomerTag(customerTag);

        //Assert
        Assert.IsTrue(_repositoryCustomerTag.Table.Any());
    }

    [TestMethod]
    public async Task InsertTagToCustomerTest()
    {
        //Arrange
        var customer = new Customer { Id = "1" };
        await _repositoryCustomer.InsertAsync(customer);

        var customerTag = new CustomerTag { Id = "1" };
        await _repositoryCustomerTag.InsertAsync(customerTag);

        //Act
        await _customerTagService.InsertTagToCustomer(customerTag.Id, customer.Id);

        //Assert
        Assert.IsTrue(_repositoryCustomer.Table.FirstOrDefault(x => x.Id == customer.Id).CustomerTags
            .Contains(customerTag.Id));
    }

    [TestMethod]
    public async Task DeleteTagFromCustomerTest()
    {
        //Arrange
        var customer = new Customer { Id = "1" };
        customer.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer);

        var customerTag = new CustomerTag { Id = "1" };
        await _repositoryCustomerTag.InsertAsync(customerTag);

        //Act
        await _customerTagService.DeleteTagFromCustomer(customerTag.Id, customer.Id);

        //Assert
        Assert.IsFalse(_repositoryCustomer.Table.FirstOrDefault(x => x.Id == customer.Id).CustomerTags
            .Contains(customerTag.Id));
    }

    [TestMethod]
    public async Task UpdateCustomerTagTest()
    {
        //Assert
        var customerTag = new CustomerTag { Id = "1", Name = "test" };
        await _customerTagService.InsertCustomerTag(customerTag);

        //Act
        customerTag.Name = "test2";
        await _customerTagService.UpdateCustomerTag(customerTag);

        //Assert
        Assert.IsTrue(_repositoryCustomerTag.Table.FirstOrDefault(x => x.Id == customerTag.Id).Name == "test2");
    }

    [TestMethod]
    public async Task GetCustomerCountTest()
    {
        //Arrange
        var customer1 = new Customer();
        customer1.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer1);
        var customer2 = new Customer();
        customer2.CustomerTags.Add("2");
        await _repositoryCustomer.InsertAsync(customer2);
        var customer3 = new Customer();
        customer3.CustomerTags.Add("1");
        await _repositoryCustomer.InsertAsync(customer3);

        //Act
        var result = await _customerTagService.GetCustomerCount("1");

        //Assert
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public async Task GetCustomerTagProductsTest()
    {
        //Assert
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "2" });

        //Act
        var result = await _customerTagService.GetCustomerTagProducts("1");

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetCustomerTagProductTest()
    {
        //Assert
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct
            { CustomerTagId = "1", ProductId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "2" });

        //Act
        var result = await _customerTagService.GetCustomerTagProduct("1", "1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerTagProductByIdTest()
    {
        //Assert
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct
            { CustomerTagId = "1", ProductId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { Id = "1", CustomerTagId = "1" });
        await _repositoryCustomerTagProduct.InsertAsync(new CustomerTagProduct { CustomerTagId = "2" });

        //Act
        var result = await _customerTagService.GetCustomerTagProductById("1");

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("1", result.CustomerTagId);
    }

    [TestMethod]
    public async Task InsertCustomerTagProductTest()
    {
        //Act
        await _customerTagService.InsertCustomerTagProduct(new CustomerTagProduct());

        //Assert
        Assert.IsTrue(_repositoryCustomerTagProduct.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCustomerTagProductTest()
    {
        //Assert
        var customerTagProduct = new CustomerTagProduct();
        await _repositoryCustomerTagProduct.InsertAsync(customerTagProduct);

        //Act
        customerTagProduct.DisplayOrder = 10;
        await _customerTagService.UpdateCustomerTagProduct(customerTagProduct);

        //Assert
        Assert.IsTrue(_repositoryCustomerTagProduct.Table.FirstOrDefault(x => x.Id == customerTagProduct.Id)
            .DisplayOrder == 10);
    }

    [TestMethod]
    public async Task DeleteCustomerTagProductTest()
    {
        //Assert
        var customerTagProduct = new CustomerTagProduct();
        await _repositoryCustomerTagProduct.InsertAsync(customerTagProduct);

        //Act
        await _customerTagService.DeleteCustomerTagProduct(customerTagProduct);

        //Assert
        Assert.IsFalse(_repositoryCustomerTag.Table.Any());
    }
}