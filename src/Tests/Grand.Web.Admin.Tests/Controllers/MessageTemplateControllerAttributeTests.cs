using System.Linq;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class MessageTemplateControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseMessageTemplateController()
    {
        Assert.IsTrue(typeof(BaseMessageTemplateController).IsAssignableFrom(typeof(MessageTemplateController)));
        Assert.AreEqual(typeof(BaseMessageTemplateController), typeof(MessageTemplateController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        var attr = typeof(MessageTemplateController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaAdminAttribute()
    {
        var attr = typeof(MessageTemplateController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(MessageTemplateController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(MessageTemplateController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }
}
