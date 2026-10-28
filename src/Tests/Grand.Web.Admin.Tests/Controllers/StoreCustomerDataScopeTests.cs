using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreCustomerDataScopeTests
{
    private static StoreCustomerDataScope Build(string staffStoreId, bool isRegistered = true,
        Action<Mock<IGroupService>> groups = null)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(isRegistered);
        groups?.Invoke(groupServiceMock);

        return new StoreCustomerDataScope(contextAccessorMock.Object, groupServiceMock.Object);
    }

    //a store manager manages registered customers only: a staff account in reach could be reset and taken over

    [TestMethod]
    [DataRow("admin")]
    [DataRow("vendor")]
    [DataRow("storemanager")]
    [DataRow("salesmanager")]
    public async Task HasAccess_StaffGroupMember_False(string role)
    {
        var scope = Build("store-1", groups: g => {
            g.Setup(x => x.IsAdmin(It.IsAny<Customer>())).ReturnsAsync(role == "admin");
            g.Setup(x => x.IsVendor(It.IsAny<Customer>())).ReturnsAsync(role == "vendor");
            g.Setup(x => x.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(role == "storemanager");
            g.Setup(x => x.IsSalesManager(It.IsAny<Customer>())).ReturnsAsync(role == "salesmanager");
        });

        Assert.IsFalse(await scope.HasAccess(new Customer { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task HasAccess_TiedToAVendor_False()
    {
        var scope = Build("store-1");

        Assert.IsFalse(await scope.HasAccess(new Customer { StoreId = "store-1", VendorId = "vendor-1" }));
    }

    [TestMethod]
    public async Task HasAccess_TiedToAStaffStore_False()
    {
        var scope = Build("store-1");

        Assert.IsFalse(await scope.HasAccess(new Customer { StoreId = "store-1", StaffStoreId = "store-1" }));
    }

    [TestMethod]
    public void StaffGroupSystemNames_CoverEveryStaffRole()
    {
        CollectionAssert.AreEquivalent(new[] {
            SystemCustomerGroupNames.Administrators, SystemCustomerGroupNames.Vendors,
            SystemCustomerGroupNames.StoreManager, SystemCustomerGroupNames.SalesManager
        }, StoreCustomerDataScope.StaffGroupSystemNames);
    }

    [TestMethod]
    public async Task HasAccess_MatchingStoreId_Registered_NotDeleted_True()
    {
        var scope = Build("store-1");
        var target = new Customer { StoreId = "store-1", Deleted = false };

        Assert.IsTrue(await scope.HasAccess(target));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedStoreId_False()
    {
        var scope = Build("store-1");
        var target = new Customer { StoreId = "store-2", Deleted = false };

        Assert.IsFalse(await scope.HasAccess(target));
    }

    [TestMethod]
    public async Task HasAccess_Deleted_False()
    {
        var scope = Build("store-1");
        var target = new Customer { StoreId = "store-1", Deleted = true };

        Assert.IsFalse(await scope.HasAccess(target));
    }

    [TestMethod]
    public async Task HasAccess_NotRegistered_False()
    {
        var scope = Build("store-1", isRegistered: false);
        var target = new Customer { StoreId = "store-1", Deleted = false };

        Assert.IsFalse(await scope.HasAccess(target));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build("store-1");

        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = Build("store-1");

        Assert.AreEqual("store-1", scope.DefaultStoreId);
        Assert.IsNull(scope.DefaultVendorId);
    }
}
