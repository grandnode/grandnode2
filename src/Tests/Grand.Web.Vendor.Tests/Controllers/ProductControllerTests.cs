using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Grand.Web.Vendor.Tests.Controllers;

[TestClass]
public class ProductControllerTests
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
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeVendorAttribute), true), $"{type.Name}: missing [AuthorizeVendor].");
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

    // The Vendor-only answers the shared tab controllers leave to the host. A hook that is not overridden
    // falls back to the Admin/Store behavior without a build error, so a lost override shows up only here.
    [TestMethod]
    [DataRow(typeof(ProductRelatedController), "InvalidRelatedProductAddPopupResult")]
    [DataRow(typeof(ProductRelatedController), "InvalidSimilarProductAddPopupResult")]
    [DataRow(typeof(ProductRelatedController), "InvalidCrossSellProductAddPopupResult")]
    [DataRow(typeof(ProductRelatedController), "InvalidRecommendedProductAddPopupResult")]
    [DataRow(typeof(ProductBundleController), "InvalidBundleProductAddPopupResult")]
    [DataRow(typeof(ProductAssociatedController), "get_AssociatedProductVendorId")]
    public void ProductControllers_OverrideTheVendorHooks(Type type, string member)
    {
        var method = type.GetMethod(member, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method, $"{type.Name}.{member} not found.");
        Assert.AreEqual(type, method.DeclaringType, $"{type.Name} does not override {member}.");
    }
}
