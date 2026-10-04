using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreOrderDataScopeTests
{
    private static StoreOrderDataScope Build(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new StoreOrderDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_MatchingStoreId_True()
    {
        var scope = Build("store-1");
        Assert.IsTrue(await scope.HasAccess(new Order { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedStoreId_False()
    {
        var scope = Build("store-1");
        Assert.IsFalse(await scope.HasAccess(new Order { StoreId = "store-2" }));
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
