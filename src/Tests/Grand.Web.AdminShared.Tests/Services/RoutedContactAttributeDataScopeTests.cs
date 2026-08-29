#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.AdminShared.Tests.Services;

/// <summary>
///     Covers the routing decision itself - which concrete IAdminDataScope&lt;ContactAttribute&gt; the resolver
///     delegates to for each area, and that an unrecognized or missing area fails closed rather than
///     falling back to the unscoped global scope.
/// </summary>
[TestClass]
public class RoutedContactAttributeDataScopeTests
{
    private const string StaffStoreId = "store-1";

    private GlobalAdminDataScope<ContactAttribute> _globalScope = null!;
    private StoreAdminDataScope<ContactAttribute> _storeScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var contextAccessor = new Mock<IContextAccessor>();
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        _globalScope = new GlobalAdminDataScope<ContactAttribute>();
        _storeScope = new StoreAdminDataScope<ContactAttribute>(contextAccessor.Object);
    }

    private RoutedContactAttributeDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedContactAttributeDataScope(httpContextAccessor.Object, _globalScope, _storeScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToGlobalScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
        Assert.IsNull(resolver.DefaultStoreId);
        Assert.IsTrue(resolver.ShowStoreSelector);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
        Assert.AreEqual(StaffStoreId, resolver.DefaultStoreId);
        Assert.IsTrue(resolver.ShowStoreSelector);
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
        var resolver = ResolverForArea("Vendor");
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void NoHttpContext_Throws()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);
        var resolver = new RoutedContactAttributeDataScope(httpContextAccessor.Object, _globalScope, _storeScope);
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }
}
