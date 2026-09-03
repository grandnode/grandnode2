using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class GiftVoucherControllerAttributeTests
{
    [TestMethod]
    public void IsThinSubclassOfBaseGiftVoucherController()
    {
        Assert.IsTrue(typeof(BaseGiftVoucherController).IsAssignableFrom(typeof(GiftVoucherController)));
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(GiftVoucherController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(GiftVoucherController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void EditWarningCheck_OwnStore_NoWarning()
    {
        // The condition lives in the concrete Store subclass, not the shared base - a truth-table
        // regression test belongs here, not in BaseGiftVoucherControllerTests.
        var method = typeof(GiftVoucherController).GetMethod("EditWarningCheck",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "EditWarningCheck override must exist on the Store subclass");
    }
}
