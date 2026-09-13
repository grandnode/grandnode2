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

    [TestMethod]
    public async Task GetActiveStore_ShouldReturnNull_WhenNoStoresExist()
    {
        // Arrange — ARCH-001 GetActiveStore() consolidation: the original implementation dereferenced
        // FirstOrDefault() with the null-forgiving operator here, throwing a NullReferenceException on a
        // zero-store install. Must degrade gracefully instead, matching BaseAdminController's original
        // (already-safe) behavior.
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());

        // Act
        var result = await _adminStoreService.GetActiveStore();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetActiveStore_ShouldReturnEmptyString_WithoutLookup_WhenContextStoreIdIsEmpty()
    {
        // Arrange — ARCH-001 GetActiveStore() consolidation: an empty scope means "all stores"; must
        // short-circuit before calling GetStoreById("") rather than relying on it happening to return
        // null for an empty id.
        var stores = new List<Store> { new Store { Id = "store1" }, new Store { Id = "store2" } };
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(stores);

        var customer = new Customer { CustomerGuid = Guid.NewGuid() };
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);

        // Act
        var result = await _adminStoreService.GetActiveStore();

        // Assert
        Assert.AreEqual(string.Empty, result);
        _storeServiceMock.Verify(s => s.GetStoreById(It.IsAny<string>()), Times.Never);
    }
}