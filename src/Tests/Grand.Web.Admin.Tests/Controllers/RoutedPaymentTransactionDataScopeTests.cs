#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedPaymentTransactionDataScopeTests
{
    private const string StaffStoreId = "store-1";

    private GlobalAdminDataScope<PaymentTransaction> _adminScope = null!;
    private StorePaymentTransactionDataScope _storeScope = null!;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        _adminScope = new GlobalAdminDataScope<PaymentTransaction>();
        _storeScope = new StorePaymentTransactionDataScope(contextAccessor.Object);
    }

    private RoutedPaymentTransactionDataScope ResolverForArea(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null) httpContext.Request.RouteValues["area"] = area;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new RoutedPaymentTransactionDataScope(httpContextAccessor.Object, _adminScope, _storeScope);
    }

    [TestMethod]
    public void AdminArea_ResolvesToAdminScope()
    {
        var resolver = ResolverForArea("Admin");
        Assert.IsNull(resolver.DefaultStoreId);
    }

    [TestMethod]
    public void StoreArea_ResolvesToStoreScope()
    {
        var resolver = ResolverForArea("Store");
        Assert.AreEqual(StaffStoreId, resolver.DefaultStoreId);
    }

    [TestMethod]
    public void VendorOrUnrecognizedOrMissingArea_ThrowsFailClosed()
    {
        var vendorResolver = ResolverForArea("Vendor");
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = vendorResolver.ResourceKeyPrefix);

        var unknownResolver = ResolverForArea("Vue");
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = unknownResolver.ResourceKeyPrefix);

        var noAreaResolver = ResolverForArea(null);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = noAreaResolver.ResourceKeyPrefix);
    }
}
