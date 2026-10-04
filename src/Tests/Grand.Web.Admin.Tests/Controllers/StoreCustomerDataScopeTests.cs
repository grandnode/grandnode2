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
    private static StoreCustomerDataScope Build(string staffStoreId, bool isRegistered = true)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(isRegistered);

        return new StoreCustomerDataScope(contextAccessorMock.Object, groupServiceMock.Object);
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
