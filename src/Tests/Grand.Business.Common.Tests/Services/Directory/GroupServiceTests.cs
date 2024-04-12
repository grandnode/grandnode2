using Grand.Business.Common.Services.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class GroupServiceTests
{
    private MemoryCacheBase _cacheBase;

    private GroupService _groupService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<CustomerGroup> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<CustomerGroup>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _groupService = new GroupService(_repository, _cacheBase, _mediatorMock.Object);
    }


    [TestMethod]
    public async Task GetCustomerGroupByIdTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup();
        await _repository.InsertAsync(customerGroup);
        //Act
        var result = await _groupService.GetCustomerGroupById(customerGroup.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetCustomerGroupBySystemNameTest()
    {
        //Arrange
        const string systemName = "system";
        var customerGroup = new CustomerGroup { SystemName = systemName };
        await _repository.InsertAsync(customerGroup);
        //Act
        var result = await _groupService.GetCustomerGroupBySystemName(systemName);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllCustomerGroupsTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup { Active = true };
        await _repository.InsertAsync(customerGroup);
        //Act
        var result = await _groupService.GetAllCustomerGroups();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertCustomerGroupTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup();
        //Act
        await _groupService.InsertCustomerGroup(customerGroup);
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateCustomerGroupTest()
    {
        //Arrange
        const string systemName = "system";
        var customerGroup = new CustomerGroup();
        await _groupService.InsertCustomerGroup(customerGroup);
        //Act
        customerGroup.SystemName = systemName;
        await _groupService.UpdateCustomerGroup(customerGroup);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == customerGroup.Id).SystemName == systemName);
    }

    [TestMethod]
    public async Task DeleteCustomerGroupTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup();
        await _groupService.InsertCustomerGroup(customerGroup);
        //Act
        await _groupService.DeleteCustomerGroup(customerGroup);
        //Assert
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task IsStaffTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.Staff, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsStaff(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsAdminTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.Administrators, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsAdmin(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsSalesManagerTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.SalesManager, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsSalesManager(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsVendorTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.Vendors, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsVendor(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsOwnerTest()
    {
        //Arrange
        var customer = new Customer { OwnerId = "" };
        //Act
        var result = await _groupService.IsOwner(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsGuestTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.Guests, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsGuest(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsRegisteredTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup
            { IsSystem = true, SystemName = SystemCustomerGroupNames.Registered, Active = true };
        await _groupService.InsertCustomerGroup(customerGroup);
        var customer = new Customer();
        customer.Groups.Add(customerGroup.Id);
        //Act
        var result = await _groupService.IsRegistered(customer);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GetAllByIdsTest()
    {
        //Arrange
        var customerGroup = new CustomerGroup();
        await _repository.InsertAsync(customerGroup);
        //Act
        var result = await _groupService.GetAllByIds([customerGroup.Id]);
        //Assert
        Assert.AreEqual(1, result.Count);
    }
}