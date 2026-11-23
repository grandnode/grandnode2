using Grand.Web.Admin.Controllers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authorization;
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

    // Changing the user fields of an order is an edit, so a group whose order permission denies Edit may not do it
    [TestMethod]
    public void OrderEditUserFields_RequiresTheEditAction()
    {
        var method = typeof(BaseOrderManagementController).GetMethod("EditUserFields")!;
        var attribute = (PermissionAuthorizeActionAttribute)Attribute.GetCustomAttribute(method,
            typeof(PermissionAuthorizeActionAttribute));

        Assert.IsNotNull(attribute);
        Assert.AreEqual(PermissionActionName.Edit, attribute.PermissionAction);
    }
}
