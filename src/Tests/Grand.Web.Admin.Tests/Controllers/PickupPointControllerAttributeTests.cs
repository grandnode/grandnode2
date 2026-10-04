using System.Reflection;
using Grand.Web.Admin.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PickupPointControllerAttributeTests
{
    private static readonly Type ControllerType = typeof(PickupPointController);

    [TestMethod]
    public void Controller_HasAreaAdmin() =>
        Assert.AreEqual("Admin", ControllerType.GetCustomAttribute<AreaAttribute>()?.RouteValue);

    [TestMethod]
    public void Controller_HasAuthorizeAdmin() =>
        Assert.IsNotNull(ControllerType.GetCustomAttribute<AuthorizeAdminAttribute>());

    [TestMethod]
    public void Controller_HasAutoValidateAntiforgeryToken() =>
        Assert.IsNotNull(ControllerType.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>());

    [TestMethod]
    public void Controller_HasAuthorizeMenu() =>
        Assert.IsNotNull(ControllerType.GetCustomAttribute<AuthorizeMenuAttribute>());

    [TestMethod]
    public void Controller_HasRouteTemplate_PreservingShippingSegment()
    {
        var route = ControllerType.GetCustomAttribute<RouteAttribute>();
        Assert.IsNotNull(route);
        Assert.AreEqual("[area]/Shipping/[action]/{id?}", route.Template);
    }

    [TestMethod]
    public void CreatePickupPoint_Get_And_Post_BothPresent()
    {
        var methods = ControllerType.GetMethods().Where(m => m.Name == "CreatePickupPoint").ToList();
        Assert.AreEqual(2, methods.Count);
        Assert.IsTrue(methods.Any(m => m.GetCustomAttribute<HttpPostAttribute>() != null));
        Assert.IsTrue(methods.Any(m => m.GetCustomAttribute<HttpGetAttribute>() != null));
    }
}
