#nullable enable

using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedVendorReviewDataScopeTests
{
    private const string VendorId = "vendor-1";

    private GlobalAdminDataScope<VendorReview> _adminScope = null!;
    private VendorVendorReviewDataScope _vendorScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentVendor).Returns(new Vendor { Id = VendorId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        _adminScope = new GlobalAdminDataScope<VendorReview>();
        _vendorScope = new VendorVendorReviewDataScope(contextAccessor.Object);
    }

    private RoutedVendorReviewDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedVendorReviewDataScope(httpContextAccessor.Object, _adminScope, _vendorScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.IsNull(resolver.DefaultStoreId);
        Assert.IsNull(resolver.DefaultVendorId);
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
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
        var resolverStore = ResolverForArea("Store");
        Assert.Throws<InvalidOperationException>(() => _ = resolverStore.ResourceKeyPrefix);

        var resolverTypo = ResolverForArea("Vendorr");
        Assert.Throws<InvalidOperationException>(() => _ = resolverTypo.ResourceKeyPrefix);

        var resolverNoArea = ResolverForArea(null);
        Assert.Throws<InvalidOperationException>(() => _ = resolverNoArea.ResourceKeyPrefix);
    }
}
