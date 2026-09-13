using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorReviewControllerRoutingTests
{
    [TestMethod]
    public void AdminVendorReviewController_InheritsBaseVendorReviewController() =>
        Assert.IsTrue(typeof(BaseVendorReviewController).IsAssignableFrom(typeof(VendorReviewController)));

    [TestMethod]
    public void AdminVendorReviewController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(VendorReviewController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void AdminVendorReviewController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(VendorReviewController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminVendorReviewController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(VendorReviewController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");

    // Regression guard: VendorSearchAutoComplete leaks other vendors' names/ids and must remain
    // Admin-only - never silently dropped from Admin, never promoted to the shared base controller.
    [TestMethod]
    public void AdminVendorReviewController_DeclaresVendorSearchAutoComplete()
    {
        var declaredMethodNames = typeof(VendorReviewController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        Assert.IsTrue(declaredMethodNames.Contains("VendorSearchAutoComplete"),
            "Admin's VendorReviewController is missing VendorSearchAutoComplete.");
    }
}
