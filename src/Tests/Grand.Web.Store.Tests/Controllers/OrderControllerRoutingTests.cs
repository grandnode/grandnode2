using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class StoreControllerRoutingTests
{
    [TestMethod]
    public void StoreOrderController_InheritsBaseOrderManagementController() =>
        Assert.IsTrue(typeof(BaseOrderManagementController).IsAssignableFrom(typeof(OrderController)));

    [TestMethod]
    public void StoreOrderController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(OrderController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    // Regression guard for the defect class Task 17 caught: BaseOrderManagementController can't
    // carry a host's [Area]/[Authorize*] attributes itself (they differ per host), so each concrete
    // subclass must restate its own - a missing one here would 404 or deauthorize the whole
    // controller silently. Same shape as ProductControllerAttributesTests (Admin).
    [TestMethod]
    public void StoreOrderController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(OrderController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaStore, areaAttr.RouteValue);
    }

    [TestMethod]
    public void StoreOrderController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(OrderController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");

    [TestMethod]
    public void StoreOrderController_DoesNotDeclareAdminOnlyExportOrDeleteSelected()
    {
        var declared = typeof(OrderController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name).ToHashSet();
        Assert.IsFalse(declared.Contains("ExportExcelAll"));
        Assert.IsFalse(declared.Contains("ExportExcelSelected"));
        Assert.IsFalse(declared.Contains("DeleteSelected"));
    }
}
