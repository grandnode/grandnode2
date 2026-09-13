using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Constants = Grand.Web.Store.Extensions.Constants;

namespace Grand.Web.Store.Tests.Controllers;

/// <summary>
/// Regression test suite for attribute-family controllers' routing attributes (Store side).
/// Catches dropped [Area]/[Authorize*]/[AuthorizeMenu]/[PermissionAuthorize] attributes
/// across all 6 attribute-family entities (AddressAttribute, ContactAttribute, CustomerAttribute,
/// CheckoutAttribute, ProductAttribute, SpecificationAttribute) in the Store host.
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
    public void AllStoreControllers_InheritFromBaseControllers()
    {
        foreach (var (controller, baseType, name) in ControllerPairs)
        {
            Assert.IsTrue(baseType.IsAssignableFrom(controller),
                $"Store.{name} does not inherit from {baseType.Name}.");
        }
    }

    [TestMethod]
    public void AllStoreControllers_HaveAutoValidateAntiforgeryToken()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = typeof(AutoValidateAntiforgeryTokenAttribute)
                .IsAssignableFrom(controller)
                || controller.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false).Length > 0;

            Assert.IsTrue(hasAttr,
                $"Store.{name} is missing [AutoValidateAntiforgeryToken].");
        }
    }

    [TestMethod]
    public void AllStoreControllers_HaveAreaAttributeWithStoreArea()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(controller, typeof(AreaAttribute), false);
            Assert.IsNotNull(areaAttr, $"Store.{name} is missing [Area].");
            Assert.AreEqual(Constants.AreaStore, areaAttr.RouteValue,
                $"Store.{name}'s [Area] does not have Constants.AreaStore value.");
        }
    }

    [TestMethod]
    public void AllStoreControllers_HaveAuthorizeStoreAttribute()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = controller.IsDefined(typeof(AuthorizeStoreAttribute), false);
            Assert.IsTrue(hasAttr, $"Store.{name} is missing [AuthorizeStore].");
        }
    }

    [TestMethod]
    public void AllStoreControllers_HaveAuthorizationMenuAttribute()
    {
        foreach (var (controller, _, name) in ControllerPairs)
        {
            var hasAttr = controller.IsDefined(typeof(AuthorizeMenuAttribute), false);
            Assert.IsTrue(hasAttr, $"Store.{name} is missing [AuthorizeMenu].");
        }
    }

    [TestMethod]
    public void AllStoreControllers_HavePermissionAuthorize()
    {
        foreach (var (controller, baseType, name) in ControllerPairs)
        {
            // [PermissionAuthorize] can be on the concrete controller or the base controller
            var concreteHasAttr = controller.IsDefined(typeof(PermissionAuthorizeAttribute), false);
            var baseHasAttr = baseType.IsDefined(typeof(PermissionAuthorizeAttribute), false);

            Assert.IsTrue(concreteHasAttr || baseHasAttr,
                $"Store.{name} is missing [PermissionAuthorize] (neither on concrete nor on base).");
        }
    }

    [TestMethod]
    public void StoreCustomerAttributeController_HasPermissionAuthorizeForCustomerAttributes()
    {
        // CustomerAttribute in Store deliberately has [PermissionAuthorize(PermissionSystemName.CustomerAttributes)]
        // on the concrete Store controller, preserving a different permission value than Admin's PermissionSystemName.Settings
        var attr = (PermissionAuthorizeAttribute)Attribute.GetCustomAttribute(
            typeof(CustomerAttributeController), typeof(PermissionAuthorizeAttribute), false);

        Assert.IsNotNull(attr, "Store.CustomerAttributeController is missing [PermissionAuthorize].");
        Assert.AreEqual(PermissionSystemName.CustomerAttributes, attr.Permission,
            "Store.CustomerAttributeController's [PermissionAuthorize] should use PermissionSystemName.CustomerAttributes.");
    }
}
