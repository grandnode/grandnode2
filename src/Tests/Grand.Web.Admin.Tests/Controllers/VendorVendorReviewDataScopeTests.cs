using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorVendorReviewDataScopeTests
{
    private static VendorVendorReviewDataScope Build(string currentVendorId)
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = currentVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new VendorVendorReviewDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_MatchingVendorId_True()
    {
        var scope = Build("vendor-A");
        Assert.IsTrue(await scope.HasAccess(new VendorReview { VendorId = "vendor-A" }));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedVendorId_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(new VendorReview { VendorId = "vendor-B" }));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public async Task HasAccess_EmptyCurrentVendorIdAndEmptyEntityVendorId_False()
    {
        var scope = Build(string.Empty);
        Assert.IsFalse(await scope.HasAccess(new VendorReview { VendorId = string.Empty }));
    }

    [TestMethod]
    public async Task HasAccess_NullCurrentVendorIdAndNullEntityVendorId_False()
    {
        var scope = Build(null);
        Assert.IsFalse(await scope.HasAccess(new VendorReview { VendorId = null }));
    }

    [TestMethod]
    public void ScopeDefaults_VendorScoped()
    {
        var scope = Build("vendor-A");
        Assert.IsNull(scope.DefaultStoreId);
        Assert.AreEqual("vendor-A", scope.DefaultVendorId);
        Assert.AreEqual("Vendor", scope.ResourceKeyPrefix);
        Assert.IsFalse(scope.ShowStoreSelector);
        Assert.IsFalse(scope.CanFeatureOnHomepage);
    }
}
