extern alias StoreHost;
extern alias VendorHost;

using Grand.Domain.Permissions;
using Grand.Web.Common.Security.Authorization;
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
}
