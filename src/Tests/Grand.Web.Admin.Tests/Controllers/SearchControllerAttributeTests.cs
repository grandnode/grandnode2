using System.Linq;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class SearchControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseSearchController()
    {
        Assert.IsTrue(typeof(BaseSearchController).IsAssignableFrom(typeof(SearchController)));
        Assert.AreEqual(typeof(BaseSearchController), typeof(SearchController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeAdminAttribute()
    {
        var attr = typeof(SearchController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaAdminAttribute()
    {
        var attr = typeof(SearchController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Admin", attr.RouteValue);
    }

    [TestMethod]
    public void HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attr = typeof(SearchController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(SearchController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    // Regression guard: Index/CustomerGroup/Stores/Vendor have no Store/Vendor equivalent and must
    // stay declared directly on Admin's own concrete controller, not migrate into the shared base
    // (which would incorrectly expose them to Store/Vendor's SearchController too).
    [TestMethod]
    public void DeclaresHostOnlyPickerMethods()
    {
        var declaredMethodNames = typeof(SearchController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToHashSet();

        Assert.IsTrue(declaredMethodNames.Contains("Index"));
        Assert.IsTrue(declaredMethodNames.Contains("CustomerGroup"));
        Assert.IsTrue(declaredMethodNames.Contains("Stores"));
        Assert.IsTrue(declaredMethodNames.Contains("Vendor"));
    }
}
