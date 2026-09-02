extern alias StoreHost;

using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
/// ARCH-001 / Task 12: mandatory method-level [PermissionAuthorizeAction] regression test for
/// Discount. This is the first instance in this initiative closing the Phase 11 final-review
/// follow-up — prior phases' attribute regression tests only asserted class-level attributes
/// ([Area]/[Authorize*]/[AuthorizeMenu]/[PermissionAuthorize]); this test additionally asserts,
/// for every one of the ~40 discount actions, that the correct [PermissionAuthorizeAction] is
/// present, whether declared directly on the concrete controller or inherited from
/// BaseDiscountController.
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
        ("ProductAddPopupList", PermissionActionName.Edit),
        ("CategoryList", PermissionActionName.Preview),
        ("CategoryDelete", PermissionActionName.Edit),
        ("CategoryAddPopupList", PermissionActionName.Edit),
        ("BrandList", PermissionActionName.Preview),
        ("BrandDelete", PermissionActionName.Edit),
        ("BrandAddPopupList", PermissionActionName.Edit),
        ("CollectionList", PermissionActionName.Preview),
        ("CollectionDelete", PermissionActionName.Edit),
        ("CollectionAddPopupList", PermissionActionName.Edit),
        ("UsageHistoryList", PermissionActionName.Preview),
        ("UsageHistoryDelete", PermissionActionName.Edit)
    ];

    private static readonly (string Method, string Expected)[] AdminOnlyActionAttributes = [
        ("VendorList", PermissionActionName.Preview),
        ("VendorDelete", PermissionActionName.Edit),
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
