using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ReportsControllerRoutingTests
{
    [TestMethod]
    public void AdminReportsController_InheritsBaseFullReportsController() =>
        Assert.IsTrue(typeof(BaseFullReportsController).IsAssignableFrom(typeof(ReportsController)));

    [TestMethod]
    public void AdminReportsController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(ReportsController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminReportsController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(ReportsController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");

    [TestMethod]
    public void AdminReportsController_HasPermissionAuthorizeReportsAttribute()
    {
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(typeof(ReportsController),
            typeof(PermissionAuthorizeAttribute), false);
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.Reports, attr!.Permission);
    }

    [TestMethod]
    public void AdminReportsController_DeclaresPopularSearchTermsReport_NotOnEitherSharedBase()
    {
        var declaredDirectly = typeof(ReportsController).GetMethod("PopularSearchTermsReport",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
        Assert.IsNotNull(declaredDirectly, "PopularSearchTermsReport must be declared directly on Admin's ReportsController.");
        Assert.IsNull(typeof(BaseReportsController).GetMethod("PopularSearchTermsReport"),
            "PopularSearchTermsReport must not exist on BaseReportsController.");
        Assert.IsNull(typeof(BaseFullReportsController).GetMethod("PopularSearchTermsReport",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly),
            "PopularSearchTermsReport must not be (re)declared on BaseFullReportsController.");
    }
}
