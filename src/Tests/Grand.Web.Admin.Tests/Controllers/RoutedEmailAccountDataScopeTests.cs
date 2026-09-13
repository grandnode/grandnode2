#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedEmailAccountDataScopeTests
{
    private static IHttpContextAccessor CreateHttpContextAccessor(string? area)
    {
        var httpContext = new DefaultHttpContext();
        if (area is not null)
            httpContext.Request.RouteValues["area"] = area;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    private static StoreEmailAccountDataScope CreateStoreScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreEmailAccountDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task AdminArea_ResolvesToGlobalScope_HasAccessAlwaysTrue()
    {
        var routed = new RoutedEmailAccountDataScope(
            CreateHttpContextAccessor("Admin"),
            new GlobalAdminDataScope<EmailAccount>(),
            CreateStoreScope("store-1"));

        var account = new EmailAccount { StoreId = "store-2" };
        Assert.IsTrue(await routed.HasAccess(account));
        Assert.IsNull(routed.DefaultStoreId);
    }

    [TestMethod]
    public async Task StoreArea_ResolvesToStoreScope_HasAccessScoped()
    {
        var routed = new RoutedEmailAccountDataScope(
            CreateHttpContextAccessor("Store"),
            new GlobalAdminDataScope<EmailAccount>(),
            CreateStoreScope("store-1"));

        var owned = new EmailAccount { StoreId = "store-1" };
        var notOwned = new EmailAccount { StoreId = "store-2" };
        Assert.IsTrue(await routed.HasAccess(owned));
        Assert.IsFalse(await routed.HasAccess(notOwned));
        Assert.AreEqual("store-1", routed.DefaultStoreId);
    }

    [TestMethod]
    public async Task UnrecognizedArea_ThrowsInvalidOperationException()
    {
        var routed = new RoutedEmailAccountDataScope(
            CreateHttpContextAccessor("Vendor"),
            new GlobalAdminDataScope<EmailAccount>(),
            CreateStoreScope("store-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => routed.HasAccess(new EmailAccount { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task MissingArea_ThrowsInvalidOperationException()
    {
        var routed = new RoutedEmailAccountDataScope(
            CreateHttpContextAccessor(null),
            new GlobalAdminDataScope<EmailAccount>(),
            CreateStoreScope("store-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => routed.HasAccess(new EmailAccount { StoreId = "store-1" }));
    }
}
