extern alias WebVendor;

using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
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

    /// <summary>These 8 actions are declared on BaseFullReportsController. They are absent from Vendor
    /// specifically because Vendor's concrete type inherits BaseReportsController directly, not
    /// BaseFullReportsController (asserted separately above). Do not lump PopularSearchTermsReport in
    /// here: it is absent from Vendor for an unrelated reason (see
    /// VendorReportsController_HasNoPopularSearchTermsReport below).</summary>
    [TestMethod]
    public void VendorReportsController_HasNoBaseFullReportsControllerActions()
    {
        string[] baseFullReportsControllerOnlyActions = [
            "ReportOrderPeriodList", "ReportOrderTimeChart", "OrderAverageReportList", "ReportLatestOrder",
            "OrderIncompleteReportList", "ReportBestCustomersByNumberOfOrdersList",
            "ReportRegisteredCustomersList", "ReportCustomerTimeChart"
        ];
        foreach (var actionName in baseFullReportsControllerOnlyActions)
            Assert.IsNull(typeof(VendorReportsController).GetMethod(actionName),
                $"{actionName} is declared on BaseFullReportsController and must not exist on Vendor's " +
                "ReportsController (or any of its base types), because Vendor does not inherit " +
                "BaseFullReportsController.");
    }

    /// <summary>Unlike the 8 actions above, PopularSearchTermsReport is never declared on either shared
    /// base (see AdminReportsController_DeclaresPopularSearchTermsReport_NotOnEitherSharedBase in
    /// ReportsControllerRoutingTests.cs) — it is written directly on Admin's own concrete controller
    /// only, and is Admin-only (also absent from Store's controller, not Admin/Store-shared). It is
    /// absent from Vendor for this distinct reason, not because of the BaseFullReportsController
    /// split.</summary>
    [TestMethod]
    public void VendorReportsController_HasNoPopularSearchTermsReport() =>
        Assert.IsNull(typeof(VendorReportsController).GetMethod("PopularSearchTermsReport"),
            "PopularSearchTermsReport is Admin-only (never declared on any shared base) and must not " +
            "exist on Vendor's ReportsController.");

    [TestMethod]
    public void VendorReportsController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(VendorReportsController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual("Vendor", areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorReportsController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(VendorReportsController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");

    [TestMethod]
    public void VendorReportsController_HasPermissionAuthorizeReportsAttribute()
    {
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(
            typeof(VendorReportsController), typeof(PermissionAuthorizeAttribute), false);
        Assert.IsNotNull(attr, "Missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.Reports, attr!.Permission);
    }
}
