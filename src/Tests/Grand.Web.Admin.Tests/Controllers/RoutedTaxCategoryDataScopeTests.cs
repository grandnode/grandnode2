#nullable enable

using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedTaxCategoryDataScopeTests
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

    private static StoreTaxCategoryDataScope CreateStoreScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreTaxCategoryDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task AdminArea_ResolvesToGlobalScope_HasAccessAlwaysTrue()
    {
        var routed = new RoutedTaxCategoryDataScope(
            CreateHttpContextAccessor("Admin"),
            new GlobalAdminDataScope<TaxCategory>(),
            CreateStoreScope("store-1"));

        var category = new TaxCategory { StoreId = "store-2" };
        Assert.IsTrue(await routed.HasAccess(category));
        Assert.IsNull(routed.DefaultStoreId);
    }

    [TestMethod]
    public async Task StoreArea_ResolvesToStoreScope_HasAccessScoped()
    {
        var routed = new RoutedTaxCategoryDataScope(
            CreateHttpContextAccessor("Store"),
            new GlobalAdminDataScope<TaxCategory>(),
            CreateStoreScope("store-1"));

        var owned = new TaxCategory { StoreId = "store-1" };
        var notOwned = new TaxCategory { StoreId = "store-2" };
        Assert.IsTrue(await routed.HasAccess(owned));
        Assert.IsFalse(await routed.HasAccess(notOwned));
        Assert.AreEqual("store-1", routed.DefaultStoreId);
    }

    [TestMethod]
    public async Task UnrecognizedArea_ThrowsInvalidOperationException()
    {
        var routed = new RoutedTaxCategoryDataScope(
            CreateHttpContextAccessor("Vendor"),
            new GlobalAdminDataScope<TaxCategory>(),
            CreateStoreScope("store-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => routed.HasAccess(new TaxCategory { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task MissingArea_ThrowsInvalidOperationException()
    {
        var routed = new RoutedTaxCategoryDataScope(
            CreateHttpContextAccessor(null),
            new GlobalAdminDataScope<TaxCategory>(),
            CreateStoreScope("store-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => routed.HasAccess(new TaxCategory { StoreId = "store-1" }));
    }
}
