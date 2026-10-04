using System.Linq;
using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

// Mirrors BrandControllerTests.cs's own precedent (the immediately-preceding phase to get this
// right on first attempt) — cross-checked directly against the sibling ProductController's own
// attribute set, not re-derived from memory of what "should" be there.
//
// Deviation from the task-10 brief: the brief's draft checked for
// Microsoft.AspNetCore.Authorization.AuthorizeAttribute, but [AuthorizeAdmin] is
// Grand.Web.Common.Filters.AuthorizeAdminAttribute, a TypeFilterAttribute that does NOT derive
// from AuthorizeAttribute — so that check would read 0 matches both before AND after the
// cutover and could never catch the regression it exists to catch. Checked directly against
// AuthorizeAdminAttribute/AuthorizeMenuAttribute instead, matching BrandControllerTests.cs.
[TestClass]
public class CustomerControllerAttributeTests
{
    [TestMethod]
    public void CustomerController_CarriesAreaAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AreaAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length,
            "CustomerController must restate [Area] — BaseCustomerManagementController can't carry it (host-agnostic, lives in AdminShared).");
        Assert.AreEqual("Admin", ((AreaAttribute)attr[0]).RouteValue);
    }

    [TestMethod]
    public void CustomerController_CarriesAuthorizeAdminAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AuthorizeAdminAttribute), inherit: false);
        Assert.IsTrue(attr.Length >= 1,
            "CustomerController must restate [AuthorizeAdmin] — it previously arrived via BaseAdminController, which the new shared base can't extend.");
    }

    [TestMethod]
    public void CustomerController_CarriesAuthorizeMenuAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.IsTrue(attr.Length >= 1,
            "CustomerController must restate [AuthorizeMenu] — same transitive-base problem as [AuthorizeAdmin].");
    }

    [TestMethod]
    public void CustomerController_CarriesAutoValidateAntiforgeryToken()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        // inherit: true is fine here — BaseCustomerController already states this one directly
        // (Task 4), so it's expected to arrive via inheritance, unlike Area/AuthorizeAdmin/AuthorizeMenu.
        Assert.IsTrue(attr.Length >= 1);
    }

    [TestMethod]
    public void CustomerController_InheritsBaseCustomerManagementController_NotJustBaseCustomerController()
    {
        // Admin gets the FULL surface (Impersonate/Export/Notes/etc.) — verify it inherited the
        // management subclass, not just the shared base, so a future refactor can't silently
        // narrow Admin's own action surface without a compile-time signal here.
        Assert.IsTrue(typeof(BaseCustomerManagementController).IsAssignableFrom(typeof(CustomerController)));
    }

    [TestMethod]
    public void CustomerController_IsThinSubclass_DeclaresNoOwnActionMethods()
    {
        var ownMethods = typeof(CustomerController).GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.AreEqual(0, ownMethods.Length,
            "CustomerController should declare zero action methods of its own — everything lives in BaseCustomerManagementController/BaseCustomerController.");
    }

    // Regression test for the disclosed bug fix noted on BaseCustomerController.Create(POST):
    // pre-consolidation, Admin's own Create(POST) required PermissionActionName.Edit while
    // everything else on Create required .Create (same defect class as GiftVoucher's, see
    // GiftVoucherControllerAttributeTests.CreatePost_RequiresCreatePermission). Pin the fixed
    // permission via reflection so a future edit can't silently regress it.
    [TestMethod]
    public void CreatePost_RequiresCreatePermission()
    {
        var method = typeof(BaseCustomerController).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == "Create" && m.GetParameters().Length == 2
                         && m.GetParameters()[0].ParameterType == typeof(CustomerModel));

        var attr = method.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), inherit: false)
            .Cast<PermissionAuthorizeActionAttribute>().Single();

        Assert.AreEqual(PermissionActionName.Create, attr.PermissionAction);
    }
}
