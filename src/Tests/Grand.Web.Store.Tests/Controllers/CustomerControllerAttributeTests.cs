using System.Reflection;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

// Mirrors the Admin equivalent (Task 10, CustomerControllerAttributeTests.cs) and the same
// deviation it recorded: the task-11 brief's draft checked for
// Microsoft.AspNetCore.Authorization.AuthorizeAttribute for [AuthorizeStore], but AuthorizeStore
// is Grand.Web.Common.Filters.AuthorizeStoreAttribute, a TypeFilterAttribute that does NOT derive
// from AuthorizeAttribute (verified directly against AuthorizeStoreAttribute.cs) — so that check
// would read 0 matches both before AND after the cutover and could never catch the regression it
// exists to catch. Checked directly against AuthorizeStoreAttribute/AuthorizeMenuAttribute instead.
[TestClass]
public class CustomerControllerAttributeTests
{
    [TestMethod]
    public void CustomerController_CarriesAreaAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AreaAttribute), inherit: false);
        Assert.AreEqual(1, attr.Length,
            "CustomerController must restate [Area] — BaseCustomerController can't carry it (host-agnostic, lives in AdminShared).");
        Assert.AreEqual("Store", ((AreaAttribute)attr[0]).RouteValue);
    }

    [TestMethod]
    public void CustomerController_CarriesAuthorizeStoreAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AuthorizeStoreAttribute), inherit: false);
        Assert.IsTrue(attr.Length >= 1,
            "CustomerController must restate [AuthorizeStore] — it previously arrived via BaseStoreController, which the new shared base can't extend.");
    }

    [TestMethod]
    public void CustomerController_CarriesAuthorizeMenuAttribute()
    {
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AuthorizeMenuAttribute), inherit: false);
        Assert.IsTrue(attr.Length >= 1, "CustomerController must restate [AuthorizeMenu] — same transitive-base problem as [AuthorizeStore].");
    }

    [TestMethod]
    public void CustomerController_CarriesAutoValidateAntiforgeryToken_ViaInheritance()
    {
        // Unlike Area/AuthorizeStore/AuthorizeMenu, BaseCustomerController (AdminShared) already
        // states [AutoValidateAntiforgeryToken] directly (Task 4) — Store's original
        // BaseStoreController also carried it, but since it's no longer in the inheritance chain,
        // confirm it still arrives, now via BaseCustomerController, without Store needing to
        // restate it itself.
        var attr = typeof(CustomerController).GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: true);
        Assert.IsTrue(attr.Length >= 1);
        var ownAttr = typeof(CustomerController).GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: false);
        Assert.AreEqual(0, ownAttr.Length,
            "CustomerController should NOT restate [AutoValidateAntiforgeryToken] itself — it already arrives from BaseCustomerController.");
    }

    [TestMethod]
    public void CustomerController_DoesNotInheritBaseCustomerManagementController()
    {
        // Store gets the SUBSET surface only — verify it inherited the shared base, not the
        // Admin-only management subclass, so Impersonate/Export/etc. can never leak onto Store.
        Assert.IsFalse(typeof(Grand.Web.AdminShared.Controllers.BaseCustomerManagementController)
            .IsAssignableFrom(typeof(CustomerController)));
        Assert.IsTrue(typeof(Grand.Web.AdminShared.Controllers.BaseCustomerController)
            .IsAssignableFrom(typeof(CustomerController)));
    }

    [TestMethod]
    public void CustomerController_DeclaresOnlyItsThreeOwnMembers()
    {
        // The per-store gate override, PerStoreDisabled, and ApplyPostConstraints override are
        // genuine Store-only infrastructure — everything else must live in BaseCustomerController.
        var ownMethods = typeof(CustomerController).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var ownMethodNames = ownMethods.Select(m => m.Name)
            .Where(n => !n.StartsWith("get_") && !n.StartsWith("set_")).ToHashSet();
        var allowed = new HashSet<string> { "OnActionExecutionAsync", "PerStoreDisabled", "ApplyPostConstraints", ".ctor" };
        Assert.IsTrue(ownMethodNames.IsSubsetOf(allowed),
            $"Unexpected own members on thin Store subclass: {string.Join(", ", ownMethodNames.Except(allowed))}");
    }
}
