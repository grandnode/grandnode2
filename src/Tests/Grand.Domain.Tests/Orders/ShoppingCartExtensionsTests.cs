﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Orders.Tests
{
    [TestClass]
    public class ShoppingCartExtensionsTests
    {
        [TestMethod]
        public void RequiresShipping_ReturnExpectedResults()
        {
            var shoppingCartItems = new List<ShoppingCartItem>()
            {
                new ShoppingCartItem(){IsShipEnabled=false },
                new ShoppingCartItem(){IsShipEnabled=false },
            };

            Assert.IsFalse(shoppingCartItems.RequiresShipping());
            shoppingCartItems.Add(new ShoppingCartItem() { IsShipEnabled = true });
            Assert.IsTrue(shoppingCartItems.RequiresShipping());
        }

        [TestMethod]
        public void LimitPerStore_ReturnExpectedResults()
        {
            var shoppingCartItems = new List<ShoppingCartItem>()
            {
                new ShoppingCartItem(){IsShipEnabled=false },
                new ShoppingCartItem(){IsShipEnabled=false },
            };
            Assert.IsTrue(shoppingCartItems.LimitPerStore(false, "id").ToList().Count == 0);
            shoppingCartItems.Add(new ShoppingCartItem() { StoreId = "id" });
            var result = shoppingCartItems.LimitPerStore(false, "id").ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.First().StoreId.Equals("id"));
        }
    }
}
