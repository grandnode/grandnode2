#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
///     Covers the routing decision itself - which concrete IAdminDataScope&lt;Discount&gt; the
///     resolver delegates to for each area, and that an unrecognized or missing area fails closed
///     rather than falling back to any concrete scope. Mirrors RoutedOrderDataScopeTests: real
///     concrete scopes built with mocked IContextAccessor/IGroupService dependencies, since
///     AdminDiscountDataScope/StoreAdminDataScope&lt;Discount&gt; have no virtual members for Moq to
///     intercept.
/// </summary>
[TestClass]
public class RoutedDiscountDataScopeTests
{
    private const string StaffStoreId = "store-1";

    private AdminDiscountDataScope _adminScope = null!;
    private StoreAdminDataScope<Discount> _storeScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(false);

        _adminScope = new AdminDiscountDataScope(contextAccessor.Object, groupService.Object);
        _storeScope = new StoreAdminDataScope<Discount>(contextAccessor.Object);
    }

    private RoutedDiscountDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedDiscountDataScope(httpContextAccessor.Object, _adminScope, _storeScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.AreEqual("Admin", resolver.ResourceKeyPrefix);
        Assert.IsNull(resolver.DefaultStoreId);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual(StaffStoreId, resolver.DefaultStoreId);
    }

    [TestMethod]
    public void UnrecognizedArea_Throws()
    {
        var resolver = ResolverForArea("Vendor");
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }

    [TestMethod]
    public void MissingArea_Throws()
    {
        var resolver = ResolverForArea(null);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);
    }
}
