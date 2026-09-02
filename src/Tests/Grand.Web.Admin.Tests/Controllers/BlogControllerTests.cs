using Grand.Web.Admin.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BlogControllerTests
{
    [TestMethod]
    public void HasAreaAttribute_SetToAdmin()
    {
        var attr = typeof(BlogController).GetCustomAttributes(typeof(AreaAttribute), false)
            .Cast<AreaAttribute>().SingleOrDefault();
        Assert.IsNotNull(attr);
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        Assert.IsTrue(typeof(BlogController).GetCustomAttributes(typeof(Grand.Web.Common.Filters.AuthorizeAdminAttribute), true).Length > 0);
    }
}
