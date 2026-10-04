using System.Reflection;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BrandControllerTests
{
    [TestMethod]
    public void BrandController_IsThinSubclassOfBaseBrandController()
    {
        Assert.IsTrue(typeof(BrandController).IsSubclassOf(typeof(BaseBrandController)));
    }

    [TestMethod]
    public void BrandController_HasRequiredHostAttributes()
    {
        var type = typeof(BrandController);
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), inherit: false), "[Area] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeAdminAttribute), inherit: false), "[AuthorizeAdmin] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), inherit: false), "[AuthorizeMenu] missing");

        var area = type.GetCustomAttribute<AreaAttribute>()!;
        Assert.AreEqual("Admin", area.RouteValue);
    }
}
