using System.Linq;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ProductReviewControllerAttributeTests
{
    [TestMethod]
    public void IsThinSubclassOfBaseProductReviewController()
    {
        Assert.IsTrue(typeof(BaseProductReviewController).IsAssignableFrom(typeof(ProductReviewController)));
        Assert.AreEqual(typeof(BaseProductReviewController), typeof(ProductReviewController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        var attr = typeof(ProductReviewController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaAdminAttribute()
    {
        var attr = typeof(ProductReviewController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(ProductReviewController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(ProductReviewController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }
}
