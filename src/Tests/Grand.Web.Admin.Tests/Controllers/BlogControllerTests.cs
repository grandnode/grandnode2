using System.Reflection;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BlogControllerTests
{
    [TestMethod]
    public void BlogController_IsThinSubclassOfBaseBlogController()
    {
        Assert.IsTrue(typeof(BlogController).IsSubclassOf(typeof(BaseBlogController)));
    }

    [TestMethod]
    public void BlogController_HasRequiredHostAttributes()
    {
        var type = typeof(BlogController);
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), inherit: false), "[Area] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeAdminAttribute), inherit: false), "[AuthorizeAdmin] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), inherit: false), "[AuthorizeMenu] missing");
        Assert.IsTrue(type.IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: false), "[AutoValidateAntiforgeryToken] missing");

        var area = type.GetCustomAttribute<AreaAttribute>()!;
        Assert.AreEqual("Admin", area.RouteValue);
    }
}
