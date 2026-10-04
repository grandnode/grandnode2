using System.Linq;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Controllers;

[TestClass]
public class SharedViewFolderAttributeTests
{
    private static void AssertHasSharedViewFolder<T>(string expectedControllerName)
    {
        var attributes = typeof(T).GetCustomAttributes(typeof(SharedViewFolderAttribute), true)
            .Cast<SharedViewFolderAttribute>()
            .ToList();

        Assert.AreEqual(1, attributes.Count,
            $"{typeof(T).Name} should carry exactly one [SharedViewFolder] attribute.");
        Assert.AreEqual(expectedControllerName, attributes[0].ControllerName,
            $"{typeof(T).Name}'s [SharedViewFolder] should target the '{expectedControllerName}' view folder.");
    }

    [TestMethod]
    public void BaseWarehouseController_HasSharedViewFolder_Shipping()
    {
        AssertHasSharedViewFolder<BaseWarehouseController>("Shipping");
    }

    [TestMethod]
    public void BaseShippingMethodController_HasSharedViewFolder_Shipping()
    {
        AssertHasSharedViewFolder<BaseShippingMethodController>("Shipping");
    }

    [TestMethod]
    public void BaseDeliveryDateController_HasSharedViewFolder_Shipping()
    {
        AssertHasSharedViewFolder<BaseDeliveryDateController>("Shipping");
    }

    [TestMethod]
    public void BasePickupPointController_HasSharedViewFolder_Shipping()
    {
        AssertHasSharedViewFolder<BasePickupPointController>("Shipping");
    }
}
