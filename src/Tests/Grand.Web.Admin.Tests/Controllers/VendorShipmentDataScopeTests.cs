using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorShipmentDataScopeTests
{
    private static VendorShipmentDataScope Build(string currentVendorId)
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = currentVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new VendorShipmentDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_MatchingVendorId_True()
    {
        var scope = Build("vendor-A");
        Assert.IsTrue(await scope.HasAccess(new Shipment { VendorId = "vendor-A" }));
    }

    [TestMethod]
    public async Task HasAccess_MismatchedVendorId_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(new Shipment { VendorId = "vendor-B" }));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(await scope.HasAccess(null));
    }

    [TestMethod]
    public void FilterOrderItems_MixedVendorOrder_ReturnsOnlyOwnItems()
    {
        var scope = Build("vendor-A");
        var itemA1 = new OrderItem { Id = "i1", VendorId = "vendor-A" };
        var itemB = new OrderItem { Id = "i2", VendorId = "vendor-B" };
        var itemA2 = new OrderItem { Id = "i3", VendorId = "vendor-A" };

        var filtered = scope.FilterOrderItems([itemA1, itemB, itemA2]).ToList();

        CollectionAssert.AreEqual(new[] { itemA1, itemA2 }, filtered);
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
