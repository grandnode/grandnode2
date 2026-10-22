using Grand.Domain.Permissions;
using Grand.Web.Admin.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ProductControllerAttributesTests
{
    [TestMethod]
    [DataRow(typeof(ProductController))]
    [DataRow(typeof(ProductAssociatedController))]
    [DataRow(typeof(ProductAttributeCombinationController))]
    [DataRow(typeof(ProductAttributeMappingController))]
    [DataRow(typeof(ProductBidController))]
    [DataRow(typeof(ProductBundleController))]
    [DataRow(typeof(ProductCategoryController))]
    [DataRow(typeof(ProductCollectionController))]
    [DataRow(typeof(ProductPictureController))]
    [DataRow(typeof(ProductPriceController))]
    [DataRow(typeof(ProductRelatedController))]
    [DataRow(typeof(ProductReservationController))]
    [DataRow(typeof(ProductSpecificationController))]
    public void ProductControllers_CarryRequiredAuthorizationAndCsrfAttributes(Type type)
    {
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeAdminAttribute), true), $"{type.Name}: missing [AuthorizeAdmin].");
        Assert.IsTrue(type.IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), true),
            $"{type.Name}: missing [AutoValidateAntiforgeryToken] - CSRF protection would be lost.");
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), true), $"{type.Name}: missing [Area].");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), true), $"{type.Name}: missing [AuthorizeMenu].");

        // Inherited from the Base*Controller in AdminShared - PermissionAuthorizeAttribute has no
        // AttributeUsage(Inherited = false), so MVC's attribute discovery (inherit: true) picks it up
        // from the base class without the subclass needing to restate it.
        var permissionAttr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(type,
            typeof(PermissionAuthorizeAttribute), true);
        Assert.IsNotNull(permissionAttr, $"{type.Name}: missing [PermissionAuthorize] (expected via inheritance from its base controller).");
        Assert.AreEqual(PermissionSystemName.Products, permissionAttr.Permission);
    }
}
