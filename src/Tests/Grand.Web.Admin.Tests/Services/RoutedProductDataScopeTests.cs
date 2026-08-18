#nullable enable

using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

/// <summary>
///     Covers the routing decision itself - which concrete IAdminDataScope&lt;Product&gt; the resolver
///     delegates to for each area, and that an unrecognized or missing area fails closed rather than
///     falling back to the unscoped global scope.
/// </summary>
[TestClass]
public class RoutedProductDataScopeTests
{
    private const string StaffStoreId = "store-1";
    private const string VendorId = "vendor-1";

    private GlobalAdminDataScope<Product> _globalScope = null!;
    private StoreAdminDataScope<Product> _storeScope = null!;
    private VendorProductDataScope _vendorScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        workContext.Setup(x => x.CurrentVendor).Returns(new Vendor { Id = VendorId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        _globalScope = new GlobalAdminDataScope<Product>();
        _storeScope = new StoreAdminDataScope<Product>(contextAccessor.Object);
        _vendorScope = new VendorProductDataScope(contextAccessor.Object);
    }

    private RoutedProductDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedProductDataScope(httpContextAccessor.Object, _globalScope, _storeScope, _vendorScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToGlobalScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
        Assert.IsNull(resolver.DefaultStoreId);
        Assert.IsNull(resolver.DefaultVendorId);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
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
    public void MissingArea_Throws()
    {
        var resolver = ResolverForArea(null);
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void UnrecognizedArea_Throws()
    {
        var resolver = ResolverForArea("Something");
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void NoHttpContext_Throws()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);
        var resolver = new RoutedProductDataScope(httpContextAccessor.Object, _globalScope, _storeScope, _vendorScope);
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }
}
