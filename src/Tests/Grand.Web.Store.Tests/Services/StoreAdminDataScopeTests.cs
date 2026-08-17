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

    // These three mirror AclMappingExtension.AccessToEntityByStore's existing, deliberately strict
    // rule (src/Web/Grand.Web.AdminShared/Extensions/AclMappingExtension.cs), the same rule
    // ProductController.CanAccessProduct already enforces for Edit(POST)/Delete/CopyProduct today
    // (see src/Tests/Grand.Web.Store.Tests/Controllers/ProductControllerTests.cs,
    // Delete_ProductNotLimitedToAnyStore_IsDenied and
    // Delete_ProductInMultipleStoresIncludingStaffStore_IsDenied, both commented
    // "counter-intuitive but current behavior... must not silently fix this"). Access is granted
    // ONLY when the product is limited to stores, is in exactly one store, and that store is the
    // staff member's store — a global product or one shared across multiple stores is denied.
    [TestMethod]
    public async Task HasAccess_ProductNotLimitedToStores_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = false };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToOtherStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = ["store-2"] };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToStaffStoreOnly_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId] };

        Assert.IsTrue(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductInMultipleStoresIncludingStaffStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId, "store-3"] };

        Assert.IsFalse(await scope.HasAccess(product));
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

    [TestMethod]
    public void ShowStoreSelector_IsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.IsTrue(scope.ShowStoreSelector);
    }

    // CanView is deliberately looser than HasAccess: it mirrors Store's original Edit(GET)/CopyProduct
    // rule (a global or multi-store product including the staff member's store may be viewed/copied;
    // only a product limited to stores that exclude the staff member's store is denied). See the
    // existing test comment in ProductControllerTests.EditGet_ProductSharedAcrossMultipleStoresIncludingStaffStore_ShowsFormWithWarning:
    // "This is the one path that must stay outside any shared 'authorize or redirect' helper."
    [TestMethod]
    public async Task CanView_ProductNotLimitedToStores_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.IsTrue(await scope.CanView(new Product { LimitedToStores = false }));
    }

    [TestMethod]
    public async Task CanView_ProductInMultipleStoresIncludingStaffStore_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId, "store-3"] };
        Assert.IsTrue(await scope.CanView(product));
    }

    [TestMethod]
    public async Task CanView_ProductLimitedToOtherStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = ["store-2"] };
        Assert.IsFalse(await scope.CanView(product));
    }
}
