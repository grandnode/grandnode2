using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Controllers;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Vendor.Tests.Controllers;

[TestClass]
public class VendorReviewControllerSurfaceTests
{
    // Regression guard for the defect class Task 17 caught: BaseVendorReviewController can't carry
    // a host's [Area]/[Authorize*] attributes itself (they differ per host), so each concrete
    // subclass must restate its own - a missing one here would 404 or deauthorize the whole
    // controller silently. Same shape as OrderControllerSurfaceTests (Vendor).
    [TestMethod]
    public void VendorVendorReviewController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(VendorReviewController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaVendor, areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorVendorReviewController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(VendorReviewController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");

    // Inverse of AdminVendorReviewController_DeclaresVendorSearchAutoComplete: this action leaks
    // other vendors' names/ids and must never be promoted or duplicated onto Vendor's controller.
    [TestMethod]
    public void VendorVendorReviewController_DoesNotDeclareVendorSearchAutoComplete()
    {
        var declaredMethodNames = typeof(VendorReviewController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        Assert.IsFalse(declaredMethodNames.Contains("VendorSearchAutoComplete"),
            "Vendor's VendorReviewController must not expose VendorSearchAutoComplete.");
    }
}
