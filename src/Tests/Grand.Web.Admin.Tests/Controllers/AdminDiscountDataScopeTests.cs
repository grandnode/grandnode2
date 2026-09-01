#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class AdminDiscountDataScopeTests
{
    private static AdminDiscountDataScope BuildScope(bool isStoreManager, string staffStoreId = "store-a")
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsStoreManager(customer)).ReturnsAsync(isStoreManager);
        return new AdminDiscountDataScope(contextAccessor.Object, groupService.Object);
    }

    [TestMethod]
    public async Task HasAccess_NotStoreManager_AlwaysTrue()
    {
        var scope = BuildScope(isStoreManager: false);
        var discount = new Discount { LimitedToStores = true, Stores = ["store-b"] };
        Assert.IsTrue(await scope.HasAccess(discount));
    }

    [TestMethod]
    public async Task HasAccess_StoreManager_DeniesOtherStoreDiscount()
    {
        var scope = BuildScope(isStoreManager: true, staffStoreId: "store-a");
        var discount = new Discount { LimitedToStores = true, Stores = ["store-b"] };
        Assert.IsFalse(await scope.HasAccess(discount));
    }

    [TestMethod]
    public async Task HasAccess_StoreManager_AllowsOwnStoreDiscount()
    {
        var scope = BuildScope(isStoreManager: true, staffStoreId: "store-a");
        var discount = new Discount { LimitedToStores = true, Stores = ["store-a"] };
        Assert.IsTrue(await scope.HasAccess(discount));
    }

    [TestMethod]
    public async Task CanView_StoreManager_GlobalDiscount_AllowedDespiteNotOwning()
    {
        var scope = BuildScope(isStoreManager: true, staffStoreId: "store-a");
        var discount = new Discount { LimitedToStores = false, Stores = [] };
        Assert.IsTrue(await scope.CanView(discount));
    }

    [TestMethod]
    public async Task CanView_StoreManager_ExclusivelyOtherStoreDiscount_Denied()
    {
        var scope = BuildScope(isStoreManager: true, staffStoreId: "store-a");
        var discount = new Discount { LimitedToStores = true, Stores = ["store-b"] };
        Assert.IsFalse(await scope.CanView(discount));
    }

    [TestMethod]
    public void DefaultStoreId_NotStoreManager_ReturnsNull()
    {
        var scope = BuildScope(isStoreManager: false);
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_StoreManager_ReturnsStaffStoreId()
    {
        var scope = BuildScope(isStoreManager: true, staffStoreId: "store-a");
        Assert.AreEqual("store-a", scope.DefaultStoreId);
    }
}
