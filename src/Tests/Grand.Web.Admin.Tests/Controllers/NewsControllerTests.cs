using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class NewsControllerTests
{
    [TestMethod]
    public void NewsController_IsThinSubclassOfBaseNewsController()
    {
        Assert.IsTrue(typeof(BaseNewsController).IsAssignableFrom(typeof(NewsController)));
        Assert.AreNotEqual(typeof(BaseNewsController), typeof(NewsController));
    }

    [TestMethod]
    public void NewsController_HasRequiredHostAttributes()
    {
        var type = typeof(NewsController);

        var areaAttr = type.GetCustomAttribute<AreaAttribute>(inherit: false);
        Assert.IsNotNull(areaAttr);
        Assert.AreEqual("Admin", areaAttr.RouteValue);

        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeMenuAttribute>(inherit: false));
        Assert.IsNotNull(type.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(inherit: false));
        // Confirm the actual namespace of AuthorizeAdminAttribute by reading the file that declares it
        // before writing this assertion - Phase 15 found it lives in Grand.Web.Common.Filters, not
        // Grand.Web.Admin.Extensions as might be assumed.
        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeAdminAttribute>(inherit: false));
    }
}
