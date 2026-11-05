using System.Reflection;
using Grand.Web.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PluginControllerHttpMethodTests
{
    [TestMethod]
    public void ReloadList_AcceptsPostOnly()
    {
        //the reload stops the application: a GET let any crawler, prefetch or copied link do it
        var method = typeof(PluginController).GetMethod(nameof(PluginController.ReloadList));
        Assert.IsNotNull(method);
        Assert.IsNotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNull(method.GetCustomAttribute<HttpGetAttribute>());
    }
}
