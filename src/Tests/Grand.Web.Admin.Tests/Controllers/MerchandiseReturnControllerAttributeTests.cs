extern alias StoreHost;
extern alias VendorHost;

using Grand.Domain.Permissions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class MerchandiseReturnControllerAttributeTests
{
    [TestMethod]
    public void AdminController_DerivesFromBaseMerchandiseReturnController()
    {
        Assert.IsTrue(typeof(Grand.Web.Admin.Controllers.MerchandiseReturnController)
            .IsSubclassOf(typeof(Grand.Web.AdminShared.Controllers.BaseMerchandiseReturnController)));
    }

    [TestMethod]
    public void StoreController_DerivesFromBaseMerchandiseReturnController()
    {
        Assert.IsTrue(typeof(StoreHost::Grand.Web.Store.Controllers.MerchandiseReturnController)
            .IsSubclassOf(typeof(Grand.Web.AdminShared.Controllers.BaseMerchandiseReturnController)));
    }

    [TestMethod]
    public void VendorController_DerivesFromBaseMerchandiseReturnController()
    {
        Assert.IsTrue(typeof(VendorHost::Grand.Web.Vendor.Controllers.MerchandiseReturnController)
            .IsSubclassOf(typeof(Grand.Web.AdminShared.Controllers.BaseMerchandiseReturnController)));
    }

    [TestMethod]
    public void BaseController_HasPermissionAuthorizeAttribute_ForMerchandiseReturnsSystemName()
    {
        var attr = typeof(Grand.Web.AdminShared.Controllers.BaseMerchandiseReturnController)
            .GetCustomAttributes(typeof(PermissionAuthorizeAttribute), inherit: false)
            .Cast<PermissionAuthorizeAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr);
        Assert.AreEqual(PermissionSystemName.MerchandiseReturns, attr.Permission);
    }

    [TestMethod]
    public void BaseController_HasAutoValidateAntiforgeryTokenAttribute()
    {
        var attrs = typeof(Grand.Web.AdminShared.Controllers.BaseMerchandiseReturnController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), inherit: false);
        Assert.AreEqual(1, attrs.Length);
    }

    // Regression guard for the defect class caught live: BaseMerchandiseReturnController can't carry a
    // host's [Area]/[Authorize*] attributes itself (they differ per host), so each concrete subclass
    // must restate its own - a missing one here would 404 or deauthorize the whole controller silently
    // (confirmed live: /Store/MerchandiseReturn/List 404'd before this fix). Same shape as
    // OrderControllerRoutingTests / PaymentTransactionControllerRoutingTests.
    [TestMethod]
    public void AdminController_HasAreaAttributeWithAdminArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(
            typeof(Grand.Web.Admin.Controllers.MerchandiseReturnController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Grand.Web.Admin.Extensions.Constants.AreaAdmin, areaAttr.RouteValue);
    }

    [TestMethod]
    public void AdminController_HasAuthorizeAdminAttribute() =>
        Assert.IsTrue(typeof(Grand.Web.Admin.Controllers.MerchandiseReturnController).IsDefined(typeof(AuthorizeAdminAttribute), false),
            "Missing [AuthorizeAdmin].");

    [TestMethod]
    public void StoreController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(
            typeof(StoreHost::Grand.Web.Store.Controllers.MerchandiseReturnController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(StoreHost::Grand.Web.Store.Extensions.Constants.AreaStore, areaAttr.RouteValue);
    }

    [TestMethod]
    public void StoreController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(StoreHost::Grand.Web.Store.Controllers.MerchandiseReturnController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");

    [TestMethod]
    public void VendorController_HasAreaAttributeWithVendorArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(
            typeof(VendorHost::Grand.Web.Vendor.Controllers.MerchandiseReturnController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(VendorHost::Grand.Web.Vendor.Extensions.Constants.AreaVendor, areaAttr.RouteValue);
    }

    [TestMethod]
    public void VendorController_HasAuthorizeVendorAttribute() =>
        Assert.IsTrue(typeof(VendorHost::Grand.Web.Vendor.Controllers.MerchandiseReturnController).IsDefined(typeof(AuthorizeVendorAttribute), false),
            "Missing [AuthorizeVendor].");
}
