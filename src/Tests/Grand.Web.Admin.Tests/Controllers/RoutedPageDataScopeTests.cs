using Grand.Domain.Pages;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedPageDataScopeTests
{
    private static RoutedPageDataScope Build(string area, out Mock<IAdminDataScope<Page>> globalMock, out Mock<IAdminDataScope<Page>> storeMock)
    {
        var httpContext = new DefaultHttpContext();
        if (area != null)
            httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var global = new GlobalAdminDataScope<Page>();
        var store = new StoreAdminDataScope<Page>(BuildContextAccessor());
        globalMock = null;
        storeMock = null;
        return new RoutedPageDataScope(httpContextAccessorMock.Object, global, store);
    }

    private static Grand.Infrastructure.IContextAccessor BuildContextAccessor()
    {
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        var contextAccessorMock = new Mock<Grand.Infrastructure.IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContext.Object);
        return contextAccessorMock.Object;
    }

    [TestMethod]
    public void DefaultStoreId_AdminArea_ResolvesToGlobalScope()
    {
        var routed = Build("Admin", out _, out _);
        Assert.IsNull(routed.DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_StoreArea_ResolvesToStoreScope()
    {
        var routed = Build("Store", out _, out _);
        Assert.AreEqual("store-1", routed.DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_VendorArea_ThrowsFailClosed()
    {
        var routed = Build("Vendor", out _, out _);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = routed.DefaultStoreId);
    }

    [TestMethod]
    public void DefaultStoreId_MissingArea_ThrowsFailClosed()
    {
        var routed = Build(null, out _, out _);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = routed.DefaultStoreId);
    }
}
