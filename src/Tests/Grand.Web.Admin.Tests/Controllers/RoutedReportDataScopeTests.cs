#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedReportDataScopeTests
{
    private AdminReportDataScope _adminScope = null!;
    private StoreReportDataScope _storeScope = null!;
    private VendorReportDataScope _vendorScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var storeWorkContext = new Mock<IWorkContext>();
        storeWorkContext.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = "store-1" });
        var storeContextAccessor = new Mock<IContextAccessor>();
        storeContextAccessor.Setup(c => c.WorkContext).Returns(storeWorkContext.Object);

        var vendorWorkContext = new Mock<IWorkContext>();
        vendorWorkContext.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = "vendor-A" });
        var vendorContextAccessor = new Mock<IContextAccessor>();
        vendorContextAccessor.Setup(c => c.WorkContext).Returns(vendorWorkContext.Object);

        _adminScope = new AdminReportDataScope();
        _storeScope = new StoreReportDataScope(storeContextAccessor.Object);
        _vendorScope = new VendorReportDataScope(vendorContextAccessor.Object);
    }

    private RoutedReportDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedReportDataScope(httpContextAccessor.Object, _adminScope, _storeScope, _vendorScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.AreEqual("", resolver.StoreId);
        Assert.IsTrue(resolver.ShowStoreSelector);
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual("store-1", resolver.StoreId);
        Assert.IsFalse(resolver.ShowStoreSelector);
    }

    [TestMethod]
    public void VendorArea_ResolvesToVendorScope()
    {
        var resolver = ResolverForArea("Vendor");
        Assert.AreEqual("vendor-A", resolver.VendorId);
        Assert.AreEqual("Vendor", resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void UnrecognizedOrMissingArea_ThrowsFailClosed()
    {
        var typo = ResolverForArea("Vendorr");
        Assert.Throws<InvalidOperationException>(() => _ = typo.ResourceKeyPrefix);

        var missing = ResolverForArea(null);
        Assert.Throws<InvalidOperationException>(() => _ = missing.ResourceKeyPrefix);
    }
}
