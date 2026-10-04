using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class ProductControllerAttributesTests
{
    [TestMethod]
    public void ProductController_CarriesRequiredAuthorizationAndCsrfAttributes()
    {
        var type = typeof(ProductController);

        Assert.IsTrue(type.IsDefined(typeof(AuthorizeStoreAttribute), true), "Missing [AuthorizeStore].");
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
