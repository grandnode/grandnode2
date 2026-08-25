using Grand.Web.AdminShared.Controllers;
using Grand.Web.Store.Controllers;
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
