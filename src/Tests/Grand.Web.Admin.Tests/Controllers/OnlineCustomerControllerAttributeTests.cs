using System.Linq;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class OnlineCustomerControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseOnlineCustomerController()
    {
        Assert.IsTrue(typeof(BaseOnlineCustomerController).IsAssignableFrom(typeof(OnlineCustomerController)));
        Assert.AreEqual(typeof(BaseOnlineCustomerController), typeof(OnlineCustomerController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        var attr = typeof(OnlineCustomerController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaAdminAttribute()
    {
        var attr = typeof(OnlineCustomerController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(OnlineCustomerController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(OnlineCustomerController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }
}
