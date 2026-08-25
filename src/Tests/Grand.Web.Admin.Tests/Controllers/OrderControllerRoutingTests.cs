using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class OrderControllerRoutingTests
{
    [TestMethod]
    public void AdminOrderController_InheritsBaseOrderManagementController() =>
        Assert.IsTrue(typeof(BaseOrderManagementController).IsAssignableFrom(typeof(OrderController)));

    [TestMethod]
    public void AdminOrderController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(OrderController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void AdminOrderController_DeclaresExportAndDeleteSelectedItself()
    {
        var declared = typeof(OrderController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name).ToHashSet();
        Assert.IsTrue(declared.Contains("ExportExcelAll"));
        Assert.IsTrue(declared.Contains("ExportExcelSelected"));
        Assert.IsTrue(declared.Contains("DeleteSelected"));
    }
}
