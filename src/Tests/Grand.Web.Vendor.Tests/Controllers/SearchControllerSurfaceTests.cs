using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Controllers;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Vendor.Tests.Controllers;

[TestClass]
public class SearchControllerSurfaceTests
{
    [TestMethod]
    public void VendorSearchController_IsSubclassOfBaseSearchController()
    {
        Assert.IsTrue(typeof(BaseSearchController).IsAssignableFrom(typeof(SearchController)));
        Assert.AreEqual(typeof(BaseSearchController), typeof(SearchController).BaseType);
    }

    // Same defect class Task 17 (Order) and Task-level reviews on later phases caught: BaseSearchController
    // can't carry a host's [Area]/[Authorize*] attributes itself (they differ per host), so each concrete
    // subclass must restate its own - a missing one here would 404 or deauthorize the whole controller
    // silently. Same shape as VendorReviewControllerSurfaceTests/OrderControllerSurfaceTests.
    [TestMethod]
    public void VendorSearchController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(SearchController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaVendor, areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorSearchController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(SearchController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");

    [TestMethod]
    public void VendorSearchController_HasAutoValidateAntiforgeryTokenAttribute() =>
        Assert.IsTrue(typeof(SearchController).IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), true),
            "Missing [AutoValidateAntiforgeryToken].");

    [TestMethod]
    public void VendorSearchController_HasAuthorizeMenuAttribute() =>
        Assert.IsTrue(typeof(SearchController).IsDefined(typeof(AuthorizeMenuAttribute), false),
            "Missing [AuthorizeMenu].");
}
