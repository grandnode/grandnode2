using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class StoreProductReviewDataScopeTests
{
    private static StoreProductReviewDataScope CreateScope(string staffStoreId)
    {
        var customer = new Customer { StaffStoreId = staffStoreId };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);
        return new StoreProductReviewDataScope(contextAccessor.Object);
    }

    [TestMethod]
    public async Task HasAccess_OwnStore_ReturnsTrue()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(new ProductReview { StoreId = "store-1" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasAccess_OtherStore_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(new ProductReview { StoreId = "store-2" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_ReturnsFalse()
    {
        var scope = CreateScope("store-1");
        var result = await scope.HasAccess(null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanView_MatchesHasAccess_OwnStore()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new ProductReview { StoreId = "store-1" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanView_MatchesHasAccess_OtherStore()
    {
        var scope = CreateScope("store-1");
        var result = await scope.CanView(new ProductReview { StoreId = "store-2" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = CreateScope("store-1");
        Assert.AreEqual("store-1", scope.DefaultStoreId);
    }

    [TestMethod]
    public void ShowStoreSelector_IsFalse()
    {
        var scope = CreateScope("store-1");
        Assert.IsFalse(scope.ShowStoreSelector);
    }
}
