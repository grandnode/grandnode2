using System.Linq;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class HomeControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseHomeControllerWithSetLanguage()
    {
        Assert.IsTrue(typeof(BaseHomeControllerWithSetLanguage).IsAssignableFrom(typeof(HomeController)));
        Assert.AreEqual(typeof(BaseHomeControllerWithSetLanguage), typeof(HomeController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(HomeController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(HomeController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(HomeController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(HomeController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    // Regression guard for BaseHomeController's own reasoning comment: SetLanguage must exist on
    // Store (it inherits BaseHomeControllerWithSetLanguage), unlike Vendor.
    [TestMethod]
    public void DeclaresSetLanguage()
    {
        var declaredMethodNames = typeof(HomeController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();
        Assert.IsTrue(declaredMethodNames.Contains("SetLanguage"));
    }
}
