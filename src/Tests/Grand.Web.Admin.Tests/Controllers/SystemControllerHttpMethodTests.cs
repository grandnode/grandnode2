using System.Reflection;
using Grand.Web.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class SystemControllerHttpMethodTests
{
    [TestMethod]
    public void RestartApplication_AcceptsPostOnly()
    {
        //a GET let any crawler, prefetch or copied link stop the application
        var method = typeof(SystemController).GetMethod(nameof(SystemController.RestartApplication));
        Assert.IsNotNull(method?.GetCustomAttribute<HttpPostAttribute>());
        Assert.IsNull(method.GetCustomAttribute<HttpGetAttribute>());
    }
}
