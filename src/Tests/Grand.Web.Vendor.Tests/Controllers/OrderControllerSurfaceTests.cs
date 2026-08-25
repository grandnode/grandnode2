using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Controllers;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Vendor.Tests.Controllers;

[TestClass]
public class OrderControllerSurfaceTests
{
    // Regression guard for ARCH-001 Order consolidation spec §3.5: Vendor's OrderController must
    // inherit BaseOrderController directly, never BaseOrderManagementController, so no mutating
    // action method exists on its type at all - not permission-gated, genuinely absent.
    private static readonly string[] ManagementOnlyActionNames = [
        "CancelOrder", "SaveOrderTags", "ChangeOrderStatus", "Delete", "EditOrderTotals",
        "EditShippingMethod", "EditUserFields", "SaveOrderItem", "DeleteOrderItem",
        "CancelOrderItem", "ResetDownloadCount", "ActivateDownloadItem", "UploadLicenseFilePopup",
        "DeleteLicenseFilePopup", "AddProductToOrder", "AddProductToOrderDetails", "AddressEdit",
        "OrderNotesSelect", "OrderNoteAdd", "OrderNoteDelete", "ExportExcelAll",
        "ExportExcelSelected", "DeleteSelected"
    ];

    [TestMethod]
    public void VendorOrderController_DoesNotInheritBaseOrderManagementController()
    {
        Assert.IsFalse(typeof(Grand.Web.AdminShared.Controllers.BaseOrderManagementController)
            .IsAssignableFrom(typeof(OrderController)));
    }

    [TestMethod]
    public void VendorOrderController_HasNoManagementOnlyActionMethods()
    {
        var declaredMethodNames = typeof(OrderController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        var leaked = ManagementOnlyActionNames.Where(declaredMethodNames.Contains).ToList();
        Assert.AreEqual(0, leaked.Count, $"Vendor's OrderController exposes management-only action(s): {string.Join(", ", leaked)}");
    }

    // Regression guard for the defect class Task 17 caught: BaseOrderController can't carry a
    // host's [Area]/[Authorize*] attributes itself (they differ per host), so each concrete
    // subclass must restate its own - a missing one here would 404 or deauthorize the whole
    // controller silently. Same shape as ProductControllerAttributesTests (Admin).
    [TestMethod]
    public void VendorOrderController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(OrderController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaVendor, areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorOrderController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(OrderController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");
}
