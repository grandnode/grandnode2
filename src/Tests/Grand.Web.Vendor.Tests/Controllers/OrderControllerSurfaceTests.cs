using Grand.Web.Vendor.Controllers;
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
}
