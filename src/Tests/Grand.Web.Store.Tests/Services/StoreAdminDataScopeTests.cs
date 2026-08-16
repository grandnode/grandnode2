using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Services;

[TestClass]
public class StoreAdminDataScopeTests
{
    private Mock<IContextAccessor> _contextAccessor = null!;
    private const string StaffStoreId = "store-1";

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
    }

    [TestMethod]
    public async Task HasAccess_ProductNotLimitedToStores_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = false };

        Assert.IsTrue(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToOtherStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = ["store-2"] };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToStaffStore_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId] };

        Assert.IsTrue(await scope.HasAccess(product));
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.AreEqual(StaffStoreId, scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsAdmin()
    {
        // Store has no distinct resource set for Product screens yet (see Task 6) — it renders
        // AdminShared's "Admin.*" keys today, so the migrated scope must keep that behavior.
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }
}
