using System.Linq;
using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Orders;
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
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    // Regression test for the disclosed bug fix noted on BaseGiftVoucherController.Create(POST):
    // pre-consolidation, Admin's own Create(POST) required PermissionActionName.Edit while
    // everything else on Create required .Create. Pin the fixed permission via reflection so a
    // future edit can't silently regress it.
    [TestMethod]
    public void CreatePost_RequiresCreatePermission()
    {
        var method = typeof(BaseGiftVoucherController).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == "Create" && m.GetParameters().Length == 2
                         && m.GetParameters()[0].ParameterType == typeof(GiftVoucherModel));

        var attr = method.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), inherit: false)
            .Cast<PermissionAuthorizeActionAttribute>().Single();

        Assert.AreEqual(PermissionActionName.Create, attr.PermissionAction);
    }
}
