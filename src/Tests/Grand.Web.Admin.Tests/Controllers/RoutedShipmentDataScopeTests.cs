#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedShipmentDataScopeTests
{
    private const string StaffStoreId = "store-1";
    private const string VendorId = "vendor-1";

    private GlobalAdminDataScope<Shipment> _adminScope = null!;
    private StoreShipmentDataScope _storeScope = null!;
    private VendorShipmentDataScope _vendorScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        workContext.Setup(x => x.CurrentVendor).Returns(new Vendor { Id = VendorId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        _adminScope = new GlobalAdminDataScope<Shipment>();
        _storeScope = new StoreShipmentDataScope(contextAccessor.Object);
        _vendorScope = new VendorShipmentDataScope(contextAccessor.Object);
    }

    private RoutedShipmentDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedShipmentDataScope(httpContextAccessor.Object, _adminScope, _storeScope, _vendorScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.IsNull(resolver.DefaultStoreId);
        Assert.IsNull(resolver.DefaultVendorId);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual(StaffStoreId, resolver.DefaultStoreId);
        Assert.IsNull(resolver.DefaultVendorId);
    }

    [TestMethod]
    public void VendorArea_ResolvesToVendorScope()
    {
        var resolver = ResolverForArea("Vendor");
        Assert.AreEqual("Vendor", resolver.ResourceKeyPrefix);
        Assert.AreEqual(VendorId, resolver.DefaultVendorId);
        Assert.IsFalse(resolver.ShowStoreSelector);
    }

    [TestMethod]
    public void UnrecognizedOrMissingArea_ThrowsFailClosed()
    {
        var resolver = ResolverForArea("Vue");
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);

        var resolverNoArea = ResolverForArea(null);
        Assert.Throws<InvalidOperationException>(() => _ = resolverNoArea.ResourceKeyPrefix);
    }
}
