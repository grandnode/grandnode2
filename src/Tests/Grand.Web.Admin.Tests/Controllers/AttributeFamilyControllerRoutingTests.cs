using Grand.Domain.Permissions;
using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

/// <summary>
/// Regression test suite for attribute-family controllers' routing attributes.
/// Catches dropped [Area]/[Authorize*]/[AuthorizeMenu]/[PermissionAuthorize] attributes
/// across all 6 attribute-family entities (AddressAttribute, ContactAttribute, CustomerAttribute,
/// CheckoutAttribute, ProductAttribute, SpecificationAttribute).
/// This is the mandatory gate from ARCH-001 Phase 11.
/// </summary>
[TestClass]
public class AttributeFamilyControllerRoutingTests
{
    private static readonly (Type Controller, Type Base, string Name)[] ControllerPairs =
    [
        (typeof(AddressAttributeController), typeof(BaseAddressAttributeController), nameof(AddressAttributeController)),
        (typeof(ContactAttributeController), typeof(BaseContactAttributeController), nameof(ContactAttributeController)),
        (typeof(CustomerAttributeController), typeof(BaseCustomerAttributeController), nameof(CustomerAttributeController)),
        (typeof(CheckoutAttributeController), typeof(BaseCheckoutAttributeController), nameof(CheckoutAttributeController)),
        (typeof(ProductAttributeController), typeof(BaseProductAttributeController), nameof(ProductAttributeController)),
        (typeof(SpecificationAttributeController), typeof(BaseSpecificationAttributeController), nameof(SpecificationAttributeController)),
    ];

    [TestMethod]
    public void AllAdminControllers_InheritFromBaseControllers()
    {
        foreach (var (controller, baseType, name) in ControllerPairs)
        {
            Assert.IsTrue(baseType.IsAssignableFrom(controller),
                $"{name} does not inherit from {baseType.Name}.");
        }
    }

    [TestMethod]
    public void AllAdminControllers_HaveAutoValidateAntiforgeryToken()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = typeof(AutoValidateAntiforgeryTokenAttribute)
                .IsAssignableFrom(controller)
                || controller.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false).Length > 0;

            Assert.IsTrue(hasAttr,
                $"{name} is missing [AutoValidateAntiforgeryToken].");
        }
    }

    [TestMethod]
    public void AllAdminControllers_HaveAreaAttributeWithAdminArea()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(controller, typeof(AreaAttribute), false);
            Assert.IsNotNull(areaAttr, $"{name} is missing [Area].");
            Assert.AreEqual(Constants.AreaAdmin, areaAttr.RouteValue,
                $"{name}'s [Area] does not have Constants.AreaAdmin value.");
        }
    }

    [TestMethod]
    public void AllAdminControllers_HaveAuthorizeAdminAttribute()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = controller.IsDefined(typeof(AuthorizeAdminAttribute), false);
            Assert.IsTrue(hasAttr, $"{name} is missing [AuthorizeAdmin].");
        }
    }

    [TestMethod]
    public void AllAdminControllers_HaveAuthorizationMenuAttribute()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = controller.IsDefined(typeof(AuthorizeMenuAttribute), false);
            Assert.IsTrue(hasAttr, $"{name} is missing [AuthorizeMenu].");
        }
    }

    [TestMethod]
    public void AllAdminControllers_HavePermissionAuthorize()
    {
        foreach (var (controller, baseType, name) in ControllerPairs)
        {
            // [PermissionAuthorize] can be on the concrete controller or the base controller
            var concreteHasAttr = controller.IsDefined(typeof(PermissionAuthorizeAttribute), false);
            var baseHasAttr = baseType.IsDefined(typeof(PermissionAuthorizeAttribute), false);

            Assert.IsTrue(concreteHasAttr || baseHasAttr,
                $"{name} is missing [PermissionAuthorize] (neither on concrete nor on base).");
        }
    }

    [TestMethod]
    public void AdminCustomerAttributeController_HasPermissionAuthorizeForSettings()
    {
        // CustomerAttribute deliberately has [PermissionAuthorize(PermissionSystemName.Settings)]
        // on the concrete Admin controller, not on the base class - this is a preserved quirk
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(
            typeof(CustomerAttributeController), typeof(PermissionAuthorizeAttribute), false);

        Assert.IsNotNull(attr, "CustomerAttributeController is missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.Settings, attr.Permission,
            "CustomerAttributeController's [PermissionAuthorize] should use PermissionSystemName.Settings.");
    }
}
