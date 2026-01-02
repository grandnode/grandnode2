using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Orders;

[TestClass]
public class ShoppingCartExtensionsTests
{
    [TestMethod]
    public void RequiresShipping_ReturnExpectedResults()
    {
        var shoppingCartItems = new List<ShoppingCartItem> {
            new() { IsShipEnabled = false },
            new() { IsShipEnabled = false }
        };

        Assert.IsFalse(shoppingCartItems.RequiresShipping());
        shoppingCartItems.Add(new ShoppingCartItem { IsShipEnabled = true });
        Assert.IsTrue(shoppingCartItems.RequiresShipping());
    }

    [TestMethod]
    public void LimitPerStore_ReturnExpectedResults()
    {
        var shoppingCartItems = new List<ShoppingCartItem> {
            new() { IsShipEnabled = false },
            new() { IsShipEnabled = false }
        };
        Assert.IsEmpty(shoppingCartItems.LimitPerStore(false, "id").ToList());
        shoppingCartItems.Add(new ShoppingCartItem { StoreId = "id" });
        var result = shoppingCartItems.LimitPerStore(false, "id").ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("id", result.First().StoreId);
    }
}