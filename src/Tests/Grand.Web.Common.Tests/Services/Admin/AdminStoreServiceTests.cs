using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Services.Admin;

[TestClass]
public class AdminStoreServiceTests
{
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;
    private AdminStoreService _adminStoreService;

    [TestInitialize]
    public void Init()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _contextAccessorMock = new Mock<IContextAccessor>();
        _adminStoreService = new AdminStoreService(_storeServiceMock.Object, _contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task GetActiveStore_ShouldReturnSingleStoreId_WhenOnlyOneStoreExists()
    {
        // Arrange
        var store = new Store { Id = "store1" };
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> { store });

        // Act
        var result = await _adminStoreService.GetActiveStore();

        // Assert
        Assert.AreEqual("store1", result);
    }

    [TestMethod]
    public async Task GetActiveStore_ShouldReturnStoreIdFromContext_WhenMultipleStoresExist()
    {
        // Arrange
        var stores = new List<Store> { new Store { Id = "store1" }, new Store { Id = "store2" } };
        var customer = new Customer { CustomerGuid = Guid.NewGuid() };
        customer.UserFields.Add(new UserField() {
            Key = SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration,
            Value = "store2",
            StoreId = ""
        });

        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(stores);
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _storeServiceMock.Setup(s => s.GetStoreById("store2")).ReturnsAsync(new Store { Id = "store2" });

        // Act
        var result = await _adminStoreService.GetActiveStore();

        // Assert
        Assert.AreEqual("store2", result);
    }

    [TestMethod]
    public async Task GetActiveStore_ShouldReturnEmptyString_WhenStoreFromContextDoesNotExist()
    {
        // Arrange
        var stores = new List<Store> { new Store { Id = "store1" }, new Store { Id = "store2" } };
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(stores);

        var customer = new Customer { CustomerGuid = Guid.NewGuid() };
        customer.UserFields.Add(new UserField() {
            Key = SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration,
            Value = "store3",
            StoreId = ""
        });
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _storeServiceMock.Setup(s => s.GetStoreById("store3")).ReturnsAsync((Store)null);

        // Act
        var result = await _adminStoreService.GetActiveStore();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}