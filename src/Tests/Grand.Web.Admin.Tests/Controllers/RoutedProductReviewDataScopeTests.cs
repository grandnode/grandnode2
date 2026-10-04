using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedProductReviewDataScopeTests
{
    private static RoutedProductReviewDataScope CreateScope(string area)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);

        var storeScope = new StoreProductReviewDataScope(contextAccessor.Object);
        return new RoutedProductReviewDataScope(httpContextAccessor.Object,
            new GlobalAdminDataScope<ProductReview>(), storeScope);
    }

    [TestMethod]
    public async Task AdminArea_ResolvesToGlobalScope_HasAccessAlwaysTrue()
    {
        var scope = CreateScope("Admin");
        var result = await scope.HasAccess(new ProductReview { StoreId = "any-other-store" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AdminArea_DefaultStoreId_IsNull()
    {
        var scope = CreateScope("Admin");
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public async Task StoreArea_ResolvesToStoreScope_HasAccessMatchesOwnership()
    {
        var scope = CreateScope("Store");
        var owned = await scope.HasAccess(new ProductReview { StoreId = "store-1" });
        var other = await scope.HasAccess(new ProductReview { StoreId = "store-2" });
        Assert.IsTrue(owned);
        Assert.IsFalse(other);
    }

    [TestMethod]
    public void StoreArea_DefaultStoreId_IsStaffStoreId()
    {
        var scope = CreateScope("Store");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }

    [TestMethod]
    public async Task UnrecognizedArea_ThrowsInvalidOperationException()
    {
        var scope = CreateScope("Vendor");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.HasAccess(new ProductReview { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task MissingArea_ThrowsInvalidOperationException()
    {
        var scope = CreateScope(null);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.HasAccess(new ProductReview { StoreId = "store-1" }));
    }
}
