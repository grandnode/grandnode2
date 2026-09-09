using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedWarehouseDataScopeTests
{
    private static RoutedWarehouseDataScope Build(string area)
    {
        var httpContext = new DefaultHttpContext();
        if (area != null)
            httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var global = new GlobalAdminDataScope<Warehouse>();
        var store = new StoreWarehouseDataScope(BuildContextAccessor());
        return new RoutedWarehouseDataScope(httpContextAccessorMock.Object, global, store);
    }

    private static IContextAccessor BuildContextAccessor()
    {
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = "store-1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContext.Object);
        return contextAccessorMock.Object;
    }

    [TestMethod]
    public void DefaultStoreId_AdminArea_ResolvesToGlobalScope()
    {
        Assert.IsNull(Build("Admin").DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_StoreArea_ResolvesToStoreScope()
    {
        Assert.AreEqual("store-1", Build("Store").DefaultStoreId);
    }

    [TestMethod]
    public void CanView_StoreArea_ForwardsToStoreScope()
    {
        // Regression guard for the Order-phase lesson: a routed scope must forward every
        // interface member, not just HasAccess/DefaultStoreId.
        var routed = Build("Store");
        Assert.IsFalse(routed.CanView(new Warehouse { StoreId = "store-2" }).Result);
    }

    [TestMethod]
    public void DefaultStoreId_VendorArea_ThrowsFailClosed()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = Build("Vendor").DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_MissingArea_ThrowsFailClosed()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = Build(null).DefaultStoreId);
    }
}
