using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PageControllerTests
{
    [TestMethod]
    public void PageController_IsThinSubclassOfBasePageController()
    {
        Assert.IsTrue(typeof(BasePageController).IsAssignableFrom(typeof(PageController)));
        Assert.AreNotEqual(typeof(BasePageController), typeof(PageController));
    }

    [TestMethod]
    public void PageController_HasRequiredHostAttributes()
    {
        var type = typeof(PageController);

        var areaAttr = type.GetCustomAttribute<AreaAttribute>(inherit: false);
        Assert.IsNotNull(areaAttr);
        Assert.AreEqual("Admin", areaAttr.RouteValue);

        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeAdminAttribute>(inherit: false));
        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeMenuAttribute>(inherit: false));
        Assert.IsNotNull(type.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(inherit: false));
    }
}
