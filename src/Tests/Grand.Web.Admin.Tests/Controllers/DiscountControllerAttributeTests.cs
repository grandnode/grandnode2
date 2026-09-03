extern alias StoreHost;

using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
/// ARCH-001 / Task 12: mandatory method-level [PermissionAuthorizeAction] regression test for
/// Discount. This is the first instance in this initiative closing the Phase 11 final-review
/// follow-up — prior phases' attribute regression tests only asserted class-level attributes
/// ([Area]/[Authorize*]/[AuthorizeMenu]/[PermissionAuthorize]); this test additionally asserts,
/// for every distinct action method name on BaseDiscountController plus Admin's Vendor region
/// (the full set of discount actions, including both GET and POST overloads of the XAddPopup
/// methods), that the correct [PermissionAuthorizeAction] is present, whether declared directly
/// on the concrete controller or inherited from BaseDiscountController.
///
/// Vendor actions (VendorList/VendorDelete/VendorAddPopup/VendorAddPopupList) exist only on
/// Grand.Web.Admin.Controllers.DiscountController (see Task 7b/9) — Store's DiscountController has
/// no equivalent methods at all, so they are asserted separately as Admin-only.
/// </summary>
[TestClass]
public class DiscountControllerAttributeTests
{
    private static readonly (string Method, string Expected)[] SharedActionAttributes = [
        ("List", PermissionActionName.List), // the POST overload specifically — List() GET has no attribute
        ("Create", PermissionActionName.Create),
        ("Edit", PermissionActionName.Preview), // GET overload; the POST overload carries Edit
        ("Delete", PermissionActionName.Delete),
        ("CouponCodeList", PermissionActionName.Preview),
        ("CouponCodeDelete", PermissionActionName.Edit),
        ("CouponCodeInsert", PermissionActionName.Edit),
        ("GetDiscountRequirementConfigurationUrl", PermissionActionName.Preview),
        ("GetDiscountRequirementMetaInfo", PermissionActionName.Preview),
        ("DeleteDiscountRequirement", PermissionActionName.Edit),
        ("ProductList", PermissionActionName.Preview),
        ("ProductDelete", PermissionActionName.Edit),
        ("ProductAddPopup", PermissionActionName.Edit),
        ("ProductAddPopupList", PermissionActionName.Edit),
        ("CategoryList", PermissionActionName.Preview),
        ("CategoryDelete", PermissionActionName.Edit),
        ("CategoryAddPopup", PermissionActionName.Edit),
        ("CategoryAddPopupList", PermissionActionName.Edit),
        ("BrandList", PermissionActionName.Preview),
        ("BrandDelete", PermissionActionName.Edit),
        ("BrandAddPopup", PermissionActionName.Edit),
        ("BrandAddPopupList", PermissionActionName.Edit),
        ("CollectionList", PermissionActionName.Preview),
        ("CollectionDelete", PermissionActionName.Edit),
        ("CollectionAddPopup", PermissionActionName.Edit),
        ("CollectionAddPopupList", PermissionActionName.Edit),
        ("UsageHistoryList", PermissionActionName.Preview),
        ("UsageHistoryDelete", PermissionActionName.Edit)
    ];

    private static readonly (string Method, string Expected)[] AdminOnlyActionAttributes = [
        ("VendorList", PermissionActionName.Preview),
        ("VendorDelete", PermissionActionName.Edit),
        ("VendorAddPopup", PermissionActionName.Edit),
        ("VendorAddPopupList", PermissionActionName.Edit)
    ];

    [TestMethod]
    public void AdminDiscountController_EveryActionMethod_HasCorrectPermissionActionName()
    {
        AssertAllActionsHaveAttribute(typeof(Grand.Web.Admin.Controllers.DiscountController), SharedActionAttributes);
        AssertAllActionsHaveAttribute(typeof(Grand.Web.Admin.Controllers.DiscountController), AdminOnlyActionAttributes);
    }

    [TestMethod]
    public void StoreDiscountController_EveryActionMethod_HasCorrectPermissionActionName()
    {
        AssertAllActionsHaveAttribute(typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController), SharedActionAttributes);
    }

    [TestMethod]
    public void StoreDiscountController_HasNoVendorActions()
    {
        var storeType = typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController);
        foreach (var (methodName, _) in AdminOnlyActionAttributes)
        {
            var exists = storeType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.Name == methodName);
            Assert.IsFalse(exists,
                $"Store's DiscountController unexpectedly has a {methodName} method — Vendor actions must remain Admin-only.");
        }
    }

    // Class-level attribute regression guard (mirrors MerchandiseReturnControllerAttributeTests /
    // AttributeFamilyControllerRoutingTests): BaseDiscountController can't carry a host's
    // [Area]/[Authorize*] attributes itself (they differ per host), so each concrete subclass must
    // restate its own - a missing one here would 404 or deauthorize the whole controller silently.
    // Admin's set was verified to match OrderController.cs:27-30 exactly; Store's set was verified
    // to match OrderController.cs:20-23 exactly (see Task 9 ledger).
    [TestMethod]
    public void AdminDiscountController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(
            typeof(Grand.Web.Admin.Controllers.DiscountController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Grand.Web.Admin.Extensions.Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminDiscountController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(Grand.Web.Admin.Controllers.DiscountController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");

    [TestMethod]
    public void AdminDiscountController_HasAutoValidateAntiforgeryTokenAttribute() =>
        Assert.IsTrue(typeof(Grand.Web.Admin.Controllers.DiscountController).IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), false),
            "Missing [AutoValidateAntiforgeryToken].");

    [TestMethod]
    public void AdminDiscountController_HasAuthorizeMenuAttribute() =>
        Assert.IsTrue(typeof(Grand.Web.Admin.Controllers.DiscountController).IsDefined(typeof(AuthorizeMenuAttribute), false),
            "Missing [AuthorizeMenu].");

    [TestMethod]
    public void StoreDiscountController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(
            typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(StoreHost::Grand.Web.Store.Extensions.Constants.AreaStore, areaAttr.RouteValue);
    }

    [TestMethod]
    public void StoreDiscountController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");

    [TestMethod]
    public void StoreDiscountController_HasAutoValidateAntiforgeryTokenAttribute() =>
        Assert.IsTrue(typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController).IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), false),
            "Missing [AutoValidateAntiforgeryToken].");

    [TestMethod]
    public void StoreDiscountController_HasAuthorizeMenuAttribute() =>
        Assert.IsTrue(typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController).IsDefined(typeof(AuthorizeMenuAttribute), false),
            "Missing [AuthorizeMenu].");

    // [PermissionAuthorize(PermissionSystemName.Discounts)] is inherited from BaseDiscountController
    // and deliberately NOT re-declared on either concrete subclass (Task 9 ledger) - assert it via
    // inherited-attribute lookup at the type level, same idiom as AssertAllActionsHaveAttribute's
    // method-level inherited lookup below.
    [TestMethod]
    public void AdminDiscountController_HasPermissionAuthorizeForDiscounts_Inherited()
    {
        var attr = typeof(Grand.Web.Admin.Controllers.DiscountController)
            .GetCustomAttributes(typeof(PermissionAuthorizeAttribute), inherit: true)
            .Cast<PermissionAuthorizeAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize] (checked with inherit: true).");
        Assert.AreEqual(PermissionSystemName.Discounts, attr.Permission);

        // Confirm it is genuinely inherited, not re-declared on the concrete class.
        Assert.IsFalse(
            typeof(Grand.Web.Admin.Controllers.DiscountController).IsDefined(typeof(PermissionAuthorizeAttribute), inherit: false),
            "PermissionAuthorize should not be re-declared on the concrete Admin controller.");
    }

    [TestMethod]
    public void StoreDiscountController_HasPermissionAuthorizeForDiscounts_Inherited()
    {
        var attr = typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController)
            .GetCustomAttributes(typeof(PermissionAuthorizeAttribute), inherit: true)
            .Cast<PermissionAuthorizeAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize] (checked with inherit: true).");
        Assert.AreEqual(PermissionSystemName.Discounts, attr.Permission);

        // Confirm it is genuinely inherited, not re-declared on the concrete class.
        Assert.IsFalse(
            typeof(StoreHost::Grand.Web.Store.Controllers.DiscountController).IsDefined(typeof(PermissionAuthorizeAttribute), inherit: false),
            "PermissionAuthorize should not be re-declared on the concrete Store controller.");
    }

    private static void AssertAllActionsHaveAttribute(Type controllerType,
        (string Method, string Expected)[] expectedAttributes)
    {
        foreach (var (methodName, expected) in expectedAttributes)
        {
            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == methodName && m.DeclaringType != typeof(object))
                .ToList();
            Assert.IsTrue(methods.Count > 0, $"{controllerType.Name}.{methodName} not found");

            //at least one overload of this method name must carry the expected attribute —
            //methods with multiple overloads (e.g. List GET/POST) are declared once on the base
            //with the attribute on only the decorated overload; find via inherited-attribute
            //lookup (inherit: true) so base-class hoisting is honored
            var hasExpected = methods.Any(m =>
                m.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), true)
                    .Cast<PermissionAuthorizeActionAttribute>()
                    .Any(a => a.PermissionAction == expected));
            Assert.IsTrue(hasExpected,
                $"{controllerType.Name}.{methodName} is missing [PermissionAuthorizeAction({expected})]");
        }
    }
}
