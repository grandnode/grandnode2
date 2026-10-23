using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class NoteAddActionsTests
{
    // Adding a note can e-mail the customer, so the action must sit behind the antiforgery check,
    // which only runs for POST.
    [TestMethod]
    [DataRow(typeof(BaseOrderManagementController), "OrderNoteAdd")]
    [DataRow(typeof(BaseShipmentController), "ShipmentNoteAdd")]
    [DataRow(typeof(BaseMerchandiseReturnController), "MerchandiseReturnNoteAdd")]
    [DataRow(typeof(CustomerNoteController), "CustomerNoteAdd")]
    [DataRow(typeof(VendorController), "VendorNoteAdd")]
    public void NoteAdd_AcceptsPostOnly(Type controller, string action)
    {
        var method = controller.GetMethod(action);

        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsDefined(typeof(HttpPostAttribute), true), $"{controller.Name}.{action}");
    }
}
