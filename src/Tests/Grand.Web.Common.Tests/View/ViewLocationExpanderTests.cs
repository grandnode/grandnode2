using System.Reflection;
using Grand.Web.Common.View;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.View;

[TestClass]
public class ViewLocationExpanderTests
{
    private static ControllerActionDescriptor DescriptorFor(Type controllerType) =>
        new() { ControllerTypeInfo = controllerType.GetTypeInfo() };

    [TestMethod]
    public void IsAdminSharedController_TypeDerivesFromAdminSharedControllersNamespace_ReturnsTrue()
    {
        var descriptor = DescriptorFor(typeof(FakeAdminSharedSubclass));
        Assert.IsTrue(ViewLocationExpander.IsAdminSharedController(descriptor));
    }

    [TestMethod]
    public void IsAdminSharedController_UnrelatedType_ReturnsFalse()
    {
        var descriptor = DescriptorFor(typeof(FakeUnrelatedController));
        Assert.IsFalse(ViewLocationExpander.IsAdminSharedController(descriptor));
    }

    [TestMethod]
    public void IsAdminSharedController_NonControllerActionDescriptor_ReturnsFalse()
    {
        var descriptor = new ActionDescriptor();
        Assert.IsFalse(ViewLocationExpander.IsAdminSharedController(descriptor));
    }

    [TestMethod]
    public void WithAdminSharedLocation_AreaAndRootLocations_InsertsBeforeRootLocations()
    {
        var locations = ViewLocationExpander.WithAdminSharedLocation([
            "/Areas/{2}/Views/{1}/{0}.cshtml",
            "/Areas/{2}/Views/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        ]).ToList();

        CollectionAssert.AreEqual(new[] {
            "/Areas/{2}/Views/{1}/{0}.cshtml",
            "/Areas/{2}/Views/Shared/{0}.cshtml",
            "/Views/AdminShared/{1}/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        }, locations);
    }

    [TestMethod]
    public void WithAdminSharedLocation_NoAreaLocations_AppendsLast()
    {
        var locations = ViewLocationExpander.WithAdminSharedLocation([
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        ]).ToList();

        Assert.AreEqual("/Views/AdminShared/{1}/{0}.cshtml", locations[^1]);
        Assert.HasCount(3, locations);
    }
}
