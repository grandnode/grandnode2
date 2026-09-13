using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class AdminReportDataScopeTests
{
    [TestMethod]
    public void ScopeDefaults_UnscopedWithBothSelectorsShown()
    {
        var scope = new AdminReportDataScope();

        Assert.AreEqual("", scope.StoreId);
        Assert.AreEqual("", scope.VendorId);
        Assert.IsTrue(scope.ShowStoreSelector);
        Assert.IsTrue(scope.ShowVendorSelector);
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }

    [TestMethod]
    public void CanIncludeProduct_NotOverridden_AlwaysTrue()
    {
        var scope = new AdminReportDataScope();
        Assert.IsTrue(scope.CanIncludeProduct(new Product { Id = "p1" }));
        Assert.IsTrue(scope.CanIncludeProduct(null));
    }
}
