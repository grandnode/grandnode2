using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedMerchandiseReturnDataScopeTests
{
    private static RoutedMerchandiseReturnDataScope Build(string area,
        IAdminDataScope<MerchandiseReturn> admin, IAdminDataScope<MerchandiseReturn> store,
        IAdminDataScope<MerchandiseReturn> vendor)
    {
        var httpContext = new DefaultHttpContext();
        if (area != null)
            httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        return new RoutedMerchandiseReturnDataScope(httpContextAccessorMock.Object,
            (GlobalAdminDataScope<MerchandiseReturn>)admin, (StoreMerchandiseReturnDataScope)store,
            (VendorMerchandiseReturnDataScope)vendor);
    }

    [TestMethod]
    public void AdminArea_ResolvesToGlobalScope()
    {
        var global = new GlobalAdminDataScope<MerchandiseReturn>();
        var routed = Build("Admin", global, new StoreMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>()),
            new VendorMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>()));

        Assert.AreEqual("Admin", routed.ResourceKeyPrefix);
        Assert.IsNull(routed.DefaultStoreId);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        var contextAccessorMock = new Mock<Grand.Infrastructure.IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContext.Object);

        var routed = Build("Store", new GlobalAdminDataScope<MerchandiseReturn>(),
            new StoreMerchandiseReturnDataScope(contextAccessorMock.Object),
            new VendorMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>()));

        Assert.AreEqual("store-1", routed.DefaultStoreId);
    }

    [TestMethod]
    public void VendorArea_ResolvesToVendorScope()
    {
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentVendor).Returns(new Grand.Domain.Vendors.Vendor { Id = "vendor-A" });
        var contextAccessorMock = new Mock<Grand.Infrastructure.IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContext.Object);

        var routed = Build("Vendor", new GlobalAdminDataScope<MerchandiseReturn>(),
            new StoreMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>()),
            new VendorMerchandiseReturnDataScope(contextAccessorMock.Object));

        Assert.AreEqual("vendor-A", routed.DefaultVendorId);
    }

    [TestMethod]
    public void UnrecognizedOrMissingArea_ThrowsFailClosed()
    {
        var admin = new GlobalAdminDataScope<MerchandiseReturn>();
        var store = new StoreMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>());
        var vendor = new VendorMerchandiseReturnDataScope(Mock.Of<Grand.Infrastructure.IContextAccessor>());

        var typo = Build("Vender", admin, store, vendor);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = typo.ResourceKeyPrefix);

        var missing = Build(null, admin, store, vendor);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = missing.ResourceKeyPrefix);
    }
}
