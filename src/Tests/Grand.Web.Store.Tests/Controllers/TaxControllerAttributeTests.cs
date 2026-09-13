using System.Linq;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class TaxControllerAttributeTests
{
    [TestMethod]
    public void IsSubclassOfBaseTaxCategoryController()
    {
        Assert.IsTrue(typeof(BaseTaxCategoryController).IsAssignableFrom(typeof(TaxController)));
        Assert.AreEqual(typeof(BaseTaxCategoryController), typeof(TaxController).BaseType);
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        var attr = typeof(TaxController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }

    [TestMethod]
    public void HasAreaStoreAttribute()
    {
        var attr = typeof(TaxController)
            .GetCustomAttributes(typeof(AreaAttribute), inherit: false)
            .Cast<AreaAttribute>().Single();
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAuthorizeMenuAttribute()
    {
        var attr = typeof(TaxController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length);
    }
}
