using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Controllers;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Vendor.Tests.Controllers;

[TestClass]
public class HomeControllerSurfaceTests
{
    // Vendor's HomeController extends the plain BaseHomeController, NOT
    // BaseHomeControllerWithSetLanguage - Vendor never had a SetLanguage action and must not gain
    // one as a side effect of this consolidation (MVC discovers every public action across the
    // whole inheritance chain, so inheriting the wrong base would silently add a new route). Same
    // shape as VendorReviewControllerSurfaceTests's DoesNotDeclareVendorSearchAutoComplete guard.
    [TestMethod]
    public void IsSubclassOfBaseHomeController()
    {
        Assert.IsTrue(typeof(BaseHomeController).IsAssignableFrom(typeof(HomeController)));
        Assert.AreEqual(typeof(BaseHomeController), typeof(HomeController).BaseType);
    }

    [TestMethod]
    public void DoesNotDeclareSetLanguage()
    {
        var declaredMethodNames = typeof(HomeController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        Assert.IsFalse(declaredMethodNames.Contains("SetLanguage"),
            "Vendor's HomeController must not expose SetLanguage - it never had this action.");
    }

    [TestMethod]
    public void VendorHomeController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(HomeController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaVendor, areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorHomeController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(HomeController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");

    [TestMethod]
    public void VendorHomeController_HasAutoValidateAntiforgeryTokenAttribute() =>
        Assert.IsTrue(typeof(HomeController).IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), true),
            "Missing [AutoValidateAntiforgeryToken].");

    [TestMethod]
    public void VendorHomeController_HasAuthorizeMenuAttribute() =>
        Assert.IsTrue(typeof(HomeController).IsDefined(typeof(AuthorizeMenuAttribute), false),
            "Missing [AuthorizeMenu].");
}
