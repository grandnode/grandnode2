using Grand.Business.Core.Interfaces.Authentication;
using Grand.Web.AdminShared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Extensions;

[TestClass]
public class PanelSignInSurfaceTests
{
    [TestMethod]
    [DataRow("Admin", SignInSurface.AdminPanel)]
    [DataRow("admin", SignInSurface.AdminPanel)]
    [DataRow("Store", SignInSurface.StorePanel)]
    [DataRow("Vendor", SignInSurface.VendorPanel)]
    [DataRow("", SignInSurface.Storefront)]
    [DataRow(null, SignInSurface.Storefront)]
    public void FromArea(string area, SignInSurface expected) =>
        Assert.AreEqual(expected, PanelSignInSurface.FromArea(area));
}
