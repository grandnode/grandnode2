using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class GiftVoucherControllerAttributeTests
{
    [TestMethod]
    public void IsThinSubclassOfBaseGiftVoucherController()
    {
        Assert.IsTrue(typeof(BaseGiftVoucherController).IsAssignableFrom(typeof(GiftVoucherController)));
        Assert.AreEqual(typeof(BaseGiftVoucherController), typeof(GiftVoucherController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaAdminAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void DoesNotHaveAuthorizeMenuAttribute()
    {
        // Admin has no [AuthorizeMenu] on this controller pre-consolidation - confirm the thin
        // subclass didn't pick one up.
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(0, attr.Length);
    }
}
