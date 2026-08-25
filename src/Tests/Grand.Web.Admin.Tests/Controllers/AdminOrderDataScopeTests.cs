using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class AdminOrderDataScopeTests
{
    private static AdminOrderDataScope Build(bool isSalesManager, string currentCustomerSeId)
    {
        var customer = new Customer { SeId = currentCustomerSeId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(g => g.IsSalesManager(customer)).ReturnsAsync(isSalesManager);

        return new AdminOrderDataScope(contextAccessorMock.Object, groupServiceMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_NotSalesManager_TrueRegardlessOfSeId()
    {
        var scope = Build(isSalesManager: false, currentCustomerSeId: "se-1");
        var order = new Order { SeId = "se-2" };

        Assert.IsTrue(await scope.HasAccess(order));
    }

    [TestMethod]
    public async Task HasAccess_SalesManager_MatchingSeId_True()
    {
        var scope = Build(isSalesManager: true, currentCustomerSeId: "se-1");
        var order = new Order { SeId = "se-1" };

        Assert.IsTrue(await scope.HasAccess(order));
    }

    [TestMethod]
    public async Task HasAccess_SalesManager_MismatchedSeId_False()
    {
        var scope = Build(isSalesManager: true, currentCustomerSeId: "se-1");
        var order = new Order { SeId = "se-2" };

        Assert.IsFalse(await scope.HasAccess(order));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build(isSalesManager: false, currentCustomerSeId: "se-1");

        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public void ScopeDefaults_MatchGlobalAdminSemantics()
    {
        var scope = Build(isSalesManager: false, currentCustomerSeId: null);

        Assert.IsNull(scope.DefaultStoreId);
        Assert.IsNull(scope.DefaultVendorId);
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
        Assert.IsTrue(scope.ShowStoreSelector);
    }
}
