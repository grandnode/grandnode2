extern alias WebVendor;

using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VendorReportsController = WebVendor::Grand.Web.Vendor.Controllers.ReportsController;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class VendorReportsControllerRoutingTests
{
    /// <summary>The core guard for this phase's one novel risk (spec §11, "the two-tier split
    /// leaking Admin/Store-only actions onto Vendor"): confirm Vendor's concrete type inherits
    /// BaseReportsController but NOT BaseFullReportsController, and therefore exposes none of the 8
    /// Admin/Store-only actions as public instance methods at all — not merely "hidden from Vendor's
    /// menu", genuinely absent from the type, so no route/Url.Action/reflection-based route dump can
    /// ever reach them on the Vendor host (Global Constraint 2/10, spec §4/§11).</summary>
    [TestMethod]
    public void VendorReportsController_InheritsBaseReportsController_NotBaseFullReportsController()
    {
        Assert.IsTrue(typeof(BaseReportsController).IsAssignableFrom(typeof(VendorReportsController)));
        Assert.IsFalse(typeof(BaseFullReportsController).IsAssignableFrom(typeof(VendorReportsController)),
            "Vendor must not inherit BaseFullReportsController — that would silently add the 8 " +
            "Admin/Store-only report routes to the Vendor host.");
    }

    [TestMethod]
    public void VendorReportsController_HasNoAdminOrStoreOnlyActions()
    {
        string[] adminStoreOnlyActions = [
            "ReportOrderPeriodList", "ReportOrderTimeChart", "OrderAverageReportList", "ReportLatestOrder",
            "OrderIncompleteReportList", "ReportBestCustomersByNumberOfOrdersList",
            "ReportRegisteredCustomersList", "ReportCustomerTimeChart", "PopularSearchTermsReport"
        ];
        foreach (var actionName in adminStoreOnlyActions)
            Assert.IsNull(typeof(VendorReportsController).GetMethod(actionName),
                $"{actionName} must not exist on Vendor's ReportsController (or any of its base types).");
    }

    [TestMethod]
    public void VendorReportsController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(VendorReportsController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual("Vendor", areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorReportsController_HasAuthorizeVendorAttribute()
    {
        var commonAssembly = System.Reflection.Assembly.Load("Grand.Web.Common");
        var authorizeVendorType = commonAssembly.GetType("Grand.Web.Common.Filters.AuthorizeVendorAttribute", false);
        Assert.IsNotNull(authorizeVendorType, "Could not load AuthorizeVendorAttribute type");
        Assert.IsTrue(typeof(VendorReportsController).IsDefined(authorizeVendorType, false),
            "Missing [AuthorizeVendor].");
    }

    [TestMethod]
    public void VendorReportsController_HasPermissionAuthorizeReportsAttribute()
    {
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(
            typeof(VendorReportsController), typeof(PermissionAuthorizeAttribute), false);
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.Reports, attr!.Permission);
    }
}
