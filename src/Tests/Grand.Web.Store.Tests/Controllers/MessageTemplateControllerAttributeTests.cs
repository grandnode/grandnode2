using System.Linq;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

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
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(MessageTemplateController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(MessageTemplateController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
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
