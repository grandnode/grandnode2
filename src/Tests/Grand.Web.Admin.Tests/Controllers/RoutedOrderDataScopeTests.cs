#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
///     Covers the routing decision itself - which concrete IAdminDataScope&lt;Order&gt; the resolver
///     delegates to for each area, and that an unrecognized or missing area fails closed rather than
///     falling back to any concrete scope. Mirrors RoutedProductDataScopeTests: real concrete scopes
///     built with mocked IContextAccessor/IGroupService dependencies, since AdminOrderDataScope/
///     StoreOrderDataScope/VendorOrderDataScope have no virtual members for Moq to intercept.
/// </summary>
[TestClass]
public class RoutedOrderDataScopeTests
{
    private const string StaffStoreId = "store-1";
    private const string VendorId = "vendor-1";

    private AdminOrderDataScope _adminScope = null!;
    private StoreOrderDataScope _storeScope = null!;
    private VendorOrderDataScope _vendorScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        workContext.Setup(x => x.CurrentVendor).Returns(new Vendor { Id = VendorId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsSalesManager(It.IsAny<Customer>())).ReturnsAsync(false);

        _adminScope = new AdminOrderDataScope(contextAccessor.Object, groupService.Object);
        _storeScope = new StoreOrderDataScope(contextAccessor.Object);
        _vendorScope = new VendorOrderDataScope(contextAccessor.Object);
    }

    private RoutedOrderDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedOrderDataScope(httpContextAccessor.Object, _adminScope, _storeScope, _vendorScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
        Assert.IsNull(resolver.DefaultStoreId);
        Assert.IsNull(resolver.DefaultVendorId);
        Assert.IsTrue(resolver.CanFeatureOnHomepage);
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
        Assert.IsFalse(resolver.CanFeatureOnHomepage);
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
