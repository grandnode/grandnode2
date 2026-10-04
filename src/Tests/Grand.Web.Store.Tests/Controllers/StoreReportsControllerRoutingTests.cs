using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoreReportsController = Grand.Web.Store.Controllers.ReportsController;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class StoreReportsControllerRoutingTests
{
    [TestMethod]
    public void StoreReportsController_InheritsBaseFullReportsController() =>
        Assert.IsTrue(typeof(BaseFullReportsController).IsAssignableFrom(typeof(StoreReportsController)));

    [TestMethod]
    public void StoreReportsController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(StoreReportsController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual("Store", areaAttr.RouteValue);
    }

    [TestMethod]
    public void StoreReportsController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(StoreReportsController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");

    [TestMethod]
    public void StoreReportsController_HasPermissionAuthorizeReportsAttribute()
    {
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(typeof(StoreReportsController),
            typeof(PermissionAuthorizeAttribute), false);
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.Reports, attr!.Permission);
    }

    [TestMethod]
    public void StoreReportsController_HasNoPopularSearchTermsReport() =>
        Assert.IsNull(typeof(StoreReportsController).GetMethod("PopularSearchTermsReport"),
            "PopularSearchTermsReport is Admin-only (Task 10) and must not exist on Store's controller.");
}
