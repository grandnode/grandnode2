using Grand.Domain.Orders;
using Grand.Web.AdminShared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class RoutedGiftVoucherDataScopeTests
{
    private static RoutedGiftVoucherDataScope CreateScope(string area, out StoreGiftVoucherDataScope storeScope)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary { ["area"] = area };
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var contextAccessor = new Mock<Grand.Infrastructure.IContextAccessor>();
        var workContext = new Mock<Grand.Infrastructure.IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Grand.Domain.Customers.Customer { StaffStoreId = "store-1" });
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);

        storeScope = new StoreGiftVoucherDataScope(contextAccessor.Object);
        return new RoutedGiftVoucherDataScope(httpContextAccessor.Object,
            new GlobalAdminDataScope<GiftVoucher>(), storeScope);
    }

    [TestMethod]
    public async Task AdminArea_ResolvesToGlobalScope_HasAccessAlwaysTrue()
    {
        var scope = CreateScope("Admin", out _);
        var result = await scope.HasAccess(new GiftVoucher { StoreId = "any-other-store" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AdminArea_DefaultStoreId_IsNull()
    {
        var scope = CreateScope("Admin", out _);
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public async Task StoreArea_ResolvesToStoreScope_HasAccessMatchesOwnership()
    {
        var scope = CreateScope("Store", out _);
        var owned = await scope.HasAccess(new GiftVoucher { StoreId = "store-1" });
        var other = await scope.HasAccess(new GiftVoucher { StoreId = "store-2" });
        Assert.IsTrue(owned);
        Assert.IsFalse(other);
    }

    [TestMethod]
    public void StoreArea_DefaultStoreId_IsStaffStoreId()
    {
        var scope = CreateScope("Store", out _);
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }

    [TestMethod]
    public async Task UnrecognizedArea_ThrowsInvalidOperationException()
    {
        var scope = CreateScope("Vendor", out _);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.HasAccess(new GiftVoucher { StoreId = "store-1" }));
    }

    [TestMethod]
    public async Task MissingArea_ThrowsInvalidOperationException()
    {
        var scope = CreateScope(null, out _);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.HasAccess(new GiftVoucher { StoreId = "store-1" }));
    }
}
