using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Vendor.Tests.Controllers;

// Regression lock for the missing-authorization-attribute bug caught during ARCH-001 Phase 1 Task 11:
// the plan's own inline example code for the thin ProductController subclass omitted
// [AuthorizeVendor]/[AutoValidateAntiforgeryToken]/[AuthorizeMenu] entirely, because BaseProductController
// can't inherit any single host's base controller and so those attributes no longer arrive
// transitively. Following the plan literally would have shipped Vendor's product management with no
// CSRF protection and no authentication/authorization filter. This test makes that class of regression
// fail loudly instead of silently the next time this controller (or one like it) is touched.
//
// All scope/access-check behavior (including Vendor's own HasAccess semantics) is now covered by
// BaseProductControllerTests, parameterized over a mocked IAdminDataScope<Product> - see
// Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs. This file only keeps routing/
// attribute-only coverage, mirroring Admin's and Store's ProductControllerAttributesTests.cs.
[TestClass]
public class ProductControllerTests
{
    [TestMethod]
    public void ProductController_CarriesRequiredAuthorizationAndCsrfAttributes()
    {
        var type = typeof(ProductController);

        Assert.IsTrue(type.IsDefined(typeof(AuthorizeVendorAttribute), true), "Missing [AuthorizeVendor].");
        Assert.IsTrue(type.IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), true),
            "Missing [AutoValidateAntiforgeryToken] - CSRF protection would be lost.");
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), true), "Missing [Area].");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), true), "Missing [AuthorizeMenu].");

        // Inherited from BaseProductController - PermissionAuthorizeAttribute has no
        // AttributeUsage(Inherited = false), so MVC's attribute discovery (inherit: true) picks it up
        // from the base class without the subclass needing to restate it.
        var permissionAttr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(type,
            typeof(PermissionAuthorizeAttribute), true);
        Assert.IsNotNull(permissionAttr, "Missing [PermissionAuthorize] (expected via inheritance from BaseProductController).");
        Assert.AreEqual(PermissionSystemName.Products, permissionAttr.Permission);
    }
}
