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
}
