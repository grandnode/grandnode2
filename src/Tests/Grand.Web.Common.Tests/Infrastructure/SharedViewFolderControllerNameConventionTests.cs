using System.Linq;
using System.Reflection;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Infrastructure;

[SharedViewFolder("TestFolder")]
public class SharedViewFolderConventionFakeControllerWithAttribute
{
}

public class SharedViewFolderConventionFakeControllerWithoutAttribute
{
}

[TestClass]
public class SharedViewFolderControllerNameConventionTests
{
    private static ControllerModel CreateControllerModel(System.Type type)
    {
        var typeInfo = type.GetTypeInfo();
        var attributes = typeInfo.GetCustomAttributes(true).ToList();
        return new ControllerModel(typeInfo, attributes);
    }

    [TestMethod]
    public void Apply_ControllerWithAttribute_SetsControllerNameFromAttribute()
    {
        var controllerModel = CreateControllerModel(typeof(SharedViewFolderConventionFakeControllerWithAttribute));
        var originalName = controllerModel.ControllerName;

        var convention = new SharedViewFolderControllerNameConvention();
        convention.Apply(controllerModel);

        Assert.AreEqual("TestFolder", controllerModel.ControllerName);
        Assert.AreNotEqual(originalName, controllerModel.ControllerName);
    }

    [TestMethod]
    public void Apply_ControllerWithoutAttribute_LeavesControllerNameUnchanged()
    {
        var controllerModel = CreateControllerModel(typeof(SharedViewFolderConventionFakeControllerWithoutAttribute));
        var originalName = controllerModel.ControllerName;

        var convention = new SharedViewFolderControllerNameConvention();
        convention.Apply(controllerModel);

        Assert.AreEqual(originalName, controllerModel.ControllerName);
    }
}
