using Grand.Business.Core.Queries.Customers;
using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerServiceTests
{
    private MemoryCacheBase _cacheBase;
    private CustomerService _customerService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Customer> _repository;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MongoDBRepositoryTest<Customer>();
        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _customerService = new CustomerService(_repository, _mediatorMock.Object, _cacheBase,
            _customerConfig);
    }

    private readonly CustomerConfig _customerConfig = new() { RegisterCustomersPerStore = true };

    //builds a service over the same in-memory repository with the per-store flag explicitly set
    private CustomerService CreateService(bool registerCustomersPerStore) =>
        new(_repository, _mediatorMock.Object, _cacheBase,
            new CustomerConfig { RegisterCustomersPerStore = registerCustomersPerStore });


    [TestMethod]
    public async Task GetOnlineCustomersTest()
    {
        //Assert
        await _repository.InsertAsync(new Customer { LastActivityDateUtc = DateTime.UtcNow.AddDays(-1) });
        await _repository.InsertAsync(new Customer { LastActivityDateUtc = DateTime.UtcNow });
        await _repository.InsertAsync(new Customer { LastActivityDateUtc = DateTime.UtcNow });
        //Act
        var result = await _customerService.GetOnlineCustomers(DateTime.UtcNow.AddMinutes(-1), null);
        //Assert
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetCountOnlineShoppingCartTest()
    {
        //Assert
        await _repository.InsertAsync(new Customer { LastActivityDateUtc = DateTime.UtcNow.AddDays(-1) });
        await _repository.InsertAsync(new Customer { LastActivityDateUtc = DateTime.UtcNow });
        var customer = new Customer { LastUpdateCartDateUtc = DateTime.UtcNow, Active = true };
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { CreatedOnUtc = DateTime.UtcNow, ShoppingCartTypeId = ShoppingCartType.ShoppingCart });
        await _repository.InsertAsync(customer);
        //Act
        var result = await _customerService.GetCountOnlineShoppingCart(DateTime.UtcNow.AddMinutes(-1), null);
        //Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task GetCustomerByIdTest()
    {
        //Assert
        await _repository.InsertAsync(new Customer { Id = "1" });
        //Act
        var result = await _customerService.GetCustomerById("1");
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomersByIdsTest()
    {
        //Assert
        await _repository.InsertAsync(new Customer { Id = "1" });
        await _repository.InsertAsync(new Customer { Id = "2" });
        await _repository.InsertAsync(new Customer { Id = "3" });
        //Act
        var result = await _customerService.GetCustomersByIds(["1", "2"]);
        //Assert
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetCustomerByGuidTest()
    {
        //Assert
        var guid = Guid.NewGuid();
        await _repository.InsertAsync(new Customer { CustomerGuid = guid });
        //Act
        var result = await _customerService.GetCustomerByGuid(guid);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmailTest()
    {
        //Assert
        var email = "email@email.com";
        await _repository.InsertAsync(new Customer { Email = email });
        //Act
        var result = await _customerService.GetCustomerByEmail(email);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerBySystemNameTest()
    {
        //Assert
        var systemName = "sample";
        await _repository.InsertAsync(new Customer { SystemName = systemName });
        //Act
        var result = await _customerService.GetCustomerBySystemName(systemName);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByUsernameTest()
    {
        //Assert
        var userName = "user";
        await _repository.InsertAsync(new Customer { Username = userName });
        //Act
        var result = await _customerService.GetCustomerByUsername(userName);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_WithStoreId_ReturnsOnlyMatchingStore()
    {
        //Arrange - same e-mail in two stores (per-store customer identity)
        const string email = "shared@email.com";
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "store-1" });
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "store-2" });
        //Act
        var result = await _customerService.GetCustomerByEmail(email, "store-2");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("store-2", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_WithStoreId_NoMatch_ReturnsNull()
    {
        //Arrange
        await _repository.InsertAsync(new Customer { Email = "shared@email.com", StoreId = "store-1" });
        //Act
        var result = await _customerService.GetCustomerByEmail("shared@email.com", "other-store");
        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByUsername_WithStoreId_ReturnsOnlyMatchingStore()
    {
        //Arrange - same username in two stores
        await _repository.InsertAsync(new Customer { Username = "user", StoreId = "store-1" });
        await _repository.InsertAsync(new Customer { Username = "user", StoreId = "store-2" });
        //Act
        var result = await _customerService.GetCustomerByUsername("user", "store-1");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("store-1", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_StoreScoped_FallsBackToStorelessAccount()
    {
        //Arrange - only the store-independent admin account exists (no customer for this store)
        const string email = "admin@email.com";
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "" });
        //Act - storefront login scoped to a store must still find the storeless admin
        var result = await _customerService.GetCustomerByEmail(email, "store-1");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_StoreScoped_PrefersStoreCustomerOverStoreless()
    {
        //Arrange - both a store customer and the storeless admin share the email
        const string email = "admin@email.com";
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "" });
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "store-1" });
        //Act
        var result = await _customerService.GetCustomerByEmail(email, "store-1");
        //Assert - the store's own customer wins within that store
        Assert.IsNotNull(result);
        Assert.AreEqual("store-1", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_GlobalLookup_PrefersStorelessAccount()
    {
        //Arrange - a store customer reused the same email as the store-independent admin account
        const string email = "admin@email.com";
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "store-2" });
        await _repository.InsertAsync(new Customer { Email = email, StoreId = "" }); //admin / back-office
        //Act - global lookup (e.g. back-office login) must not be shadowed by the store customer
        var result = await _customerService.GetCustomerByEmail(email);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("", result.StoreId);
    }

    #region GetCustomerByEmail - full branch coverage

    [TestMethod]
    public async Task GetCustomerByEmail_NullEmail_ReturnsNull()
    {
        await _repository.InsertAsync(new Customer { Email = "user@email.com" });
        var result = await _customerService.GetCustomerByEmail(null);
        Assert.IsNull(result);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task GetCustomerByEmail_EmptyOrWhitespaceEmail_ReturnsNull(string email)
    {
        await _repository.InsertAsync(new Customer { Email = "user@email.com" });
        var result = await _customerService.GetCustomerByEmail(email);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_IsCaseInsensitive()
    {
        //stored lowercased; the lookup must lowercase the input
        await _repository.InsertAsync(new Customer { Email = "user@email.com", StoreId = "" });
        var result = await _customerService.GetCustomerByEmail("USER@Email.COM");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_StoreScoped_FallsBackToAccountWithNullStoreId()
    {
        //admin created without a store leaves StoreId null (not just "") - the fallback must match it too
        await _repository.InsertAsync(new Customer { Email = "admin@email.com" });
        var result = await _customerService.GetCustomerByEmail("admin@email.com", "store-1");
        Assert.IsNotNull(result);
        Assert.IsTrue(string.IsNullOrEmpty(result.StoreId));
    }

    [TestMethod]
    public async Task GetCustomerByEmail_PerStoreOn_Global_NoStoreless_FallsBackToAnyMatch()
    {
        //only a store customer exists (no store-independent account) - global lookup still returns it
        await _repository.InsertAsync(new Customer { Email = "user@email.com", StoreId = "store-1" });
        var result = await _customerService.GetCustomerByEmail("user@email.com");
        Assert.IsNotNull(result);
        Assert.AreEqual("store-1", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_PerStoreOn_StoreScoped_NoMatchAndNoStoreless_ReturnsNull()
    {
        await _repository.InsertAsync(new Customer { Email = "user@email.com", StoreId = "store-1" });
        var result = await _customerService.GetCustomerByEmail("user@email.com", "store-2");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_PerStoreOff_StoreScoped_ReturnsExactStoreMatch()
    {
        var service = CreateService(registerCustomersPerStore: false);
        await _repository.InsertAsync(new Customer { Email = "user@email.com", StoreId = "store-1" });
        var result = await service.GetCustomerByEmail("user@email.com", "store-1");
        Assert.IsNotNull(result);
        Assert.AreEqual("store-1", result.StoreId);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_PerStoreOff_StoreScoped_DoesNotFallBackToStoreless()
    {
        var service = CreateService(registerCustomersPerStore: false);
        //a store-independent account exists, but with the flag off there is no fallback
        await _repository.InsertAsync(new Customer { Email = "admin@email.com", StoreId = "" });
        var result = await service.GetCustomerByEmail("admin@email.com", "store-1");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCustomerByEmail_PerStoreOff_Global_ReturnsMatch()
    {
        var service = CreateService(registerCustomersPerStore: false);
        await _repository.InsertAsync(new Customer { Email = "user@email.com", StoreId = "store-1" });
        var result = await service.GetCustomerByEmail("user@email.com");
        Assert.IsNotNull(result);
        Assert.AreEqual("store-1", result.StoreId);
    }

    #endregion

    [TestMethod]
    public async Task InsertGuestCustomerTest()
    {
        //Arrange
        var cg = new CustomerGroup();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetGroupBySystemNameQuery>(), default))
            .Returns(Task.FromResult(cg));

        var customer = new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = "1",
            LastActivityDateUtc = DateTime.UtcNow
        };

        //Act
        await _customerService.InsertGuestCustomer(customer);
        //Assert
        Assert.IsNotEmpty(_repository.Table);
        Assert.IsTrue(_repository.Table.Any(x => x.StoreId == "1"));
    }

    [TestMethod]
    public async Task InsertCustomerTest()
    {
        //Act
        await _customerService.InsertCustomer(new Customer());
        //Assert
        Assert.IsNotEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task UpdateCustomerFieldTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.UpdateCustomerField(customer, x => x.Email, "sample@sample.pl");
        //Assert
        Assert.AreEqual("sample@sample.pl", _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Email);
    }

    [TestMethod]
    public async Task UpdateCustomerFieldTest_Type()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.UpdateCustomerField(customer, x => x.Username, "username");
        //Assert
        Assert.AreEqual("username", _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Username);
    }

    [TestMethod]
    public async Task UpdateCustomerTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        customer.Email = "sample@sample.com";
        await _customerService.UpdateCustomer(customer);
        //Assert
        Assert.AreEqual("sample@sample.com", _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Email);
    }
    [TestMethod]
    public async Task SaveFieldTest_Insert()
    {
        //Arrange
        var customer = new Customer();
        _repository.Insert(customer);
        //Act
        await _customerService.UpdateUserField(customer, "Field", "Value");
        //Assert
        var userFields = _repository.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNotNull(userFields.FirstOrDefault(x => x.Key == "Field"));
    }

    [TestMethod]
    public async Task SaveFieldTest_Update()
    {
        //Arrange
        var customer = new Customer();
        customer.UserFields.Add(new UserField { Key = "Field", Value = "empty", StoreId = "" });
        _repository.Insert(customer);
        //Act
        await _customerService.UpdateUserField(customer, "Field", "Value");
        //Assert
        var userFields = _repository.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNotNull(userFields.FirstOrDefault(x => x.Key == "Field"));
        Assert.AreEqual("Value", userFields.FirstOrDefault(x => x.Key == "Field").Value);
    }

    [TestMethod]
    public async Task SaveFieldTest_Delete()
    {
        //Arrange
        var customer = new Customer();
        customer.UserFields.Add(new UserField { Key = "Field", Value = "Value", StoreId = "" });
        _repository.Insert(customer);
        //Act
        await _customerService.UpdateUserField(customer, "Field", string.Empty);
        //Assert
        var userFields = _repository.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNull(userFields.FirstOrDefault(x => x.Key == "Field"));
    }
    [TestMethod]
    public async Task DeleteCustomerTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.DeleteCustomer(customer);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).Deleted);
    }

    [TestMethod]
    public async Task DeleteCustomerTest_Hard()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.DeleteCustomer(customer, true);
        //Assert
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCustomerLastLoginDateTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        var date = DateTime.UtcNow;
        customer.LastLoginDateUtc = date;
        await _customerService.UpdateCustomerLastLoginDate(customer);
        //Assert
        Assert.AreEqual(date.ToString(),
            _repository.Table.FirstOrDefault(x => x.Id == customer.Id).LastLoginDateUtc.ToString());
    }

    [TestMethod]
    public async Task UpdateCustomerinAdminPanelTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        customer.AdminComment = "test";
        customer.StoreId = "store-1";
        await _customerService.UpdateCustomerInAdminPanel(customer);
        //Assert
        var updated = _repository.Table.FirstOrDefault(x => x.Id == customer.Id);
        Assert.AreEqual("test", updated.AdminComment);
        Assert.AreEqual("store-1", updated.StoreId);
    }

    [TestMethod]
    public async Task UpdateActiveTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com", Active = true };
        await _repository.InsertAsync(customer);
        //Act
        customer.Active = false;
        await _customerService.UpdateActive(customer);
        //Assert
        Assert.IsFalse(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).Active);
    }

    [TestMethod]
    public async Task UpdateContributionsTest()
    {
        //Assert
        var customer = new Customer { Email = "email@email.com" };
        await _repository.InsertAsync(customer);
        //Act
        customer.Active = false;
        await _customerService.UpdateContributions(customer);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).HasContributions);
    }

    [TestMethod]
    public async Task DeleteGuestCustomersTest()
    {
        //Arrange
        var cg = new CustomerGroup();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetGroupBySystemNameQuery>(), default))
            .Returns(Task.FromResult(cg));

        var customer = new Customer();
        customer.Groups.Add(cg.Id);
        await _repository.InsertAsync(customer);
        var customer2 = new Customer();
        customer2.Groups.Add("1");
        await _repository.InsertAsync(customer2);
        //Act
        await _customerService.DeleteGuestCustomers(null, null, false);
        //Assert
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task DeleteCustomerGroupInCustomerTest()
    {
        //Arrange
        var cg = new CustomerGroup();
        var customer = new Customer();
        customer.Groups.Add(cg.Id);
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.DeleteCustomerGroupInCustomer(cg, customer.Id);
        //Assert
        Assert.IsEmpty(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).Groups);
    }

    [TestMethod]
    public async Task InsertCustomerGroupInCustomerTest()
    {
        //Arrange
        var cg = new CustomerGroup();
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.InsertCustomerGroupInCustomer(cg, customer.Id);
        //Assert
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Groups);
    }

    [TestMethod]
    public async Task DeleteAddressTest()
    {
        //Arrange
        var address = new Address();
        var customer = new Customer();
        customer.Addresses.Add(address);
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.DeleteAddress(address, customer.Id);
        //Assert
        Assert.IsEmpty(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).Addresses);
    }

    [TestMethod]
    public async Task InsertAddressTest()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        var address = new Address();
        await _customerService.InsertAddress(address, customer.Id);
        //Assert
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Addresses);
    }

    [TestMethod]
    public async Task UpdateAddressTest()
    {
        //Arrange
        var address = new Address();
        var customer = new Customer();
        customer.Addresses.Add(address);
        await _repository.InsertAsync(customer);
        //Act
        address.Name = "sample";
        await _customerService.UpdateAddress(address, customer.Id);
        //Assert
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Addresses);
        Assert.AreEqual("sample",
            _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Addresses.FirstOrDefault(x => x.Id == address.Id)
                .Name);
    }

    [TestMethod]
    public async Task UpdateBillingAddressTest()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        var address = new Address {
            Name = "BillingAddress"
        };
        await _customerService.UpdateBillingAddress(address, customer.Id);
        //Assert
        Assert.AreEqual("BillingAddress",
            _repository.Table.FirstOrDefault(x => x.Id == customer.Id).BillingAddress.Name);
    }

    [TestMethod]
    public async Task UpdateShippingAddressTest()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        var address = new Address {
            Name = "ShippingAddress"
        };
        await _customerService.UpdateShippingAddress(address, customer.Id);
        //Assert
        Assert.AreEqual("ShippingAddress",
            _repository.Table.FirstOrDefault(x => x.Id == customer.Id).ShippingAddress.Name);
    }

    [TestMethod]
    public async Task DeleteShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        var cart = new ShoppingCartItem();
        customer.ShoppingCartItems.Add(cart);
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.DeleteShoppingCartItem(customer.Id, cart);
        //Assert
        Assert.IsEmpty(_repository.Table.FirstOrDefault(x => x.Id == customer.Id).ShoppingCartItems);
    }

    [TestMethod]
    public async Task ClearShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        var cart = new ShoppingCartItem();
        var cart2 = new ShoppingCartItem();
        customer.ShoppingCartItems.Add(cart);
        customer.ShoppingCartItems.Add(cart2);
        await _repository.InsertAsync(customer);
        //Act
        await _customerService.ClearShoppingCartItem(customer.Id, new List<ShoppingCartItem> { cart });
        //Assert
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).ShoppingCartItems);
    }

    [TestMethod]
    public async Task InsertShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        var cart = new ShoppingCartItem();
        await _customerService.InsertShoppingCartItem(customer.Id, cart);
        //Assert
        Assert.HasCount(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).ShoppingCartItems);
    }

    [TestMethod]
    public async Task UpdateShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        var cart = new ShoppingCartItem();
        customer.ShoppingCartItems.Add(cart);
        await _repository.InsertAsync(customer);
        //Act
        cart.Parameter = "test";
        await _customerService.UpdateShoppingCartItem(customer.Id, cart);
        //Assert
        Assert.AreEqual("test",
            _repository.Table.FirstOrDefault(x => x.Id == customer.Id).ShoppingCartItems
                .FirstOrDefault(x => x.Id == cart.Id).Parameter);
    }
}