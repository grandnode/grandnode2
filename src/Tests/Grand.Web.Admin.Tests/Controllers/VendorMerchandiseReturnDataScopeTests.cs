using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorMerchandiseReturnDataScopeTests
{
    private static VendorMerchandiseReturnDataScope Build(string currentVendorId)
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = currentVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new VendorMerchandiseReturnDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_MatchingVendorId_True()
    {
        var scope = Build("vendor-A");
        Assert.IsTrue(await scope.HasAccess(new MerchandiseReturn { VendorId = "vendor-A" }));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedVendorId_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(new MerchandiseReturn { VendorId = "vendor-B" }));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(null));
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
