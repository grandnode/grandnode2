#nullable enable

using System.Reflection;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
///     Covers the routing decision itself - which concrete IAdminDataScope&lt;Customer&gt; the resolver
///     delegates to for each area, and that an unrecognized or missing area fails closed rather than
///     falling back to any concrete scope. Mirrors RoutedOrderDataScopeTests/
///     RoutedMessageTemplateDataScopeTests: real concrete scopes built with mocked
///     IContextAccessor/IGroupService dependencies, since AdminCustomerDataScope/
///     StoreCustomerDataScope have no virtual members for Moq to intercept and
///     RoutedCustomerDataScope's constructor (matching every other Routed*DataScope in this
///     codebase) takes the two concrete scope types rather than IAdminDataScope&lt;Customer&gt; twice -
///     two parameters of the same interface type would make the plain
///     AddScoped&lt;IAdminDataScope&lt;Customer&gt;, RoutedCustomerDataScope&gt;() DI registration
///     ambiguous/self-referencing at resolution time.
/// </summary>
[TestClass]
public class RoutedCustomerDataScopeTests
{
    private const string StaffStoreId = "store-1";

    private AdminCustomerDataScope _adminScope = null!;
    private StoreCustomerDataScope _storeScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);

        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsSalesManager(It.IsAny<Customer>())).ReturnsAsync(false);
        groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);

        _adminScope = new AdminCustomerDataScope(contextAccessor.Object, groupService.Object);
        _storeScope = new StoreCustomerDataScope(contextAccessor.Object, groupService.Object);
    }

    private RoutedCustomerDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        return new RoutedCustomerDataScope(httpContextAccessor.Object, _adminScope, _storeScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        // Admin never scopes to a default store; Store always defaults to the staff's store -
        // the one behavioral difference between the two concrete scopes today.
        Assert.IsNull(resolver.DefaultStoreId);
    }

    [TestMethod]
    public async Task AdminArea_HasAccess_NotGatedByStoreOwnership()
    {
        var resolver = ResolverForArea("Admin");
        // AdminCustomerDataScope has no store-ownership check at all (only the Sales-Manager
        // restriction) - a customer from any store is visible.
        var result = await resolver.HasAccess(new Customer { StoreId = "some-other-store" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual(StaffStoreId, resolver.DefaultStoreId);
    }

    [TestMethod]
    public async Task StoreArea_HasAccess_GatedByStoreOwnership()
    {
        var resolver = ResolverForArea("Store");
        var owned = await resolver.HasAccess(new Customer { StoreId = StaffStoreId });
        var other = await resolver.HasAccess(new Customer { StoreId = "some-other-store" });
        Assert.IsTrue(owned);
        Assert.IsFalse(other);
    }

    [TestMethod]
    public void UnrecognizedOrMissingArea_ThrowsFailClosed()
    {
        var resolver = ResolverForArea("Vendor");
        Assert.Throws<InvalidOperationException>(() => _ = resolver.ResourceKeyPrefix);

        var resolverNoArea = ResolverForArea(null);
        Assert.Throws<InvalidOperationException>(() => _ = resolverNoArea.ResourceKeyPrefix);
    }

    [TestMethod]
    public void CanView_IsExplicitlyForwarded_NotLeftToTheInterfaceDefault()
    {
        // Regression guard: a routed scope can forward HasAccess while forgetting CanView,
        // silently falling back to the interface default (=> HasAccess) instead of a scope's real
        // CanView override - the standing lesson from VendorReview's RoutedOrderDataScope gap.
        // Customer has no CanView override on either concrete scope today, so no behavioral
        // difference between "forwards HasAccess" and "forwards CanView" is observable through a
        // real AdminCustomerDataScope/StoreCustomerDataScope - the failure mode this guards
        // against only bites once a future override lands on one of them. Verify structurally
        // instead: RoutedCustomerDataScope must declare its own CanView method rather than relying
        // on IAdminDataScope<Customer>'s default interface implementation, so that a future
        // override on either concrete scope is never silently bypassed.
        var declaresOwnCanView = typeof(RoutedCustomerDataScope)
            .GetMethod(nameof(RoutedCustomerDataScope.CanView), BindingFlags.Public | BindingFlags.Instance)
            ?.DeclaringType == typeof(RoutedCustomerDataScope);

        Assert.IsTrue(declaresOwnCanView,
            "RoutedCustomerDataScope must explicitly declare CanView, not rely on the IAdminDataScope<Customer> default.");
    }

    [TestMethod]
    public async Task CanView_ForwardsToResolvedScopesCanView()
    {
        var resolver = ResolverForArea("Store");
        var target = new Customer { StoreId = StaffStoreId };
        IAdminDataScope<Customer> storeScope = _storeScope;
        Assert.AreEqual(await storeScope.CanView(target), await resolver.CanView(target));
    }
}
