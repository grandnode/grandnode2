using Grand.Domain.Catalog;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorReportDataScopeTests
{
    private static VendorReportDataScope Build(string currentVendorId)
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Vendor { Id = currentVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        return new VendorReportDataScope(contextAccessorMock.Object);
    }

    [TestMethod]
    public void StoreId_AlwaysEmpty()
    {
        var scope = Build("vendor-A");
        Assert.AreEqual("", scope.StoreId);
    }

    [TestMethod]
    public void VendorId_ReturnsCurrentVendorId()
    {
        var scope = Build("vendor-A");
        Assert.AreEqual("vendor-A", scope.VendorId);
    }

    [TestMethod]
    public void Selectors_BothHidden()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(scope.ShowStoreSelector);
        Assert.IsFalse(scope.ShowVendorSelector);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsVendor()
    {
        var scope = Build("vendor-A");
        Assert.AreEqual("Vendor", scope.ResourceKeyPrefix);
    }

    [TestMethod]
    public void CanIncludeProduct_MatchingVendorId_True()
    {
        var scope = Build("vendor-A");
        Assert.IsTrue(scope.CanIncludeProduct(new Product { Id = "p1", VendorId = "vendor-A" }));
    }

    [TestMethod]
    public void CanIncludeProduct_MismatchedVendorId_False()
    {
        var scope = Build("vendor-A");
        Assert.IsFalse(scope.CanIncludeProduct(new Product { Id = "p1", VendorId = "vendor-B" }));
    }
}
