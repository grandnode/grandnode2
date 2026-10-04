using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class PickupPointControllerAttributeTests
{
    private static readonly Type ControllerType = typeof(PickupPointController);

    [TestMethod]
    public void Controller_HasAreaStore() =>
        Assert.AreEqual("Store", ControllerType.GetCustomAttribute<AreaAttribute>()?.RouteValue);

    [TestMethod]
    public void Controller_HasAuthorizeStore() =>
        Assert.IsNotNull(ControllerType.GetCustomAttribute<AuthorizeStoreAttribute>());

    [TestMethod]
    public void Controller_HasRouteTemplate_PreservingShippingSegment()
    {
        var route = ControllerType.GetCustomAttribute<RouteAttribute>();
        Assert.IsNotNull(route);
        Assert.AreEqual("[area]/Shipping/[action]/{id?}", route.Template);
    }

    [TestMethod]
    public void DeletePickupPoint_HasPermissionAuthorizeAction_Delete()
    {
        var method = ControllerType.GetMethod("DeletePickupPoint");
        var attr = method!.GetCustomAttribute<PermissionAuthorizeActionAttribute>();
        Assert.IsNotNull(attr);
        Assert.AreEqual(PermissionActionName.Delete, attr.PermissionAction);
    }
}
