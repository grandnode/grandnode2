using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderApprovalFingerprintTests
{
    private static ShoppingCartItem Item(string productId, int quantity = 1,
        params (string Key, string Value)[] attributes)
    {
        var item = new ShoppingCartItem {
            ProductId = productId,
            Quantity = quantity,
            ShoppingCartTypeId = ShoppingCartType.ShoppingCart
        };
        foreach (var (key, value) in attributes)
            item.Attributes.Add(new CustomAttribute { Key = key, Value = value });
        return item;
    }

    [TestMethod]
    public void Compute_SameItemsInOtherOrder_SameHash()
    {
        var first = OrderApprovalFingerprint.Compute([Item("a"), Item("b", 2), Item("a", 3, ("color", "red"))]);
        var second = OrderApprovalFingerprint.Compute([Item("a", 3, ("color", "red")), Item("a"), Item("b", 2)]);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Compute_AttributesInOtherOrder_SameHash()
    {
        var first = OrderApprovalFingerprint.Compute([Item("a", 1, ("color", "red"), ("size", "m"))]);
        var second = OrderApprovalFingerprint.Compute([Item("a", 1, ("size", "m"), ("color", "red"))]);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Compute_QuantityChanged_DifferentHash()
    {
        var before = OrderApprovalFingerprint.Compute([Item("a", 1)]);
        var after = OrderApprovalFingerprint.Compute([Item("a", 2)]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_AttributeValueChanged_DifferentHash()
    {
        var before = OrderApprovalFingerprint.Compute([Item("a", 1, ("color", "red"))]);
        var after = OrderApprovalFingerprint.Compute([Item("a", 1, ("color", "blue"))]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_AttributeMovedBetweenKeyAndValue_DifferentHash()
    {
        var before = OrderApprovalFingerprint.Compute([Item("a", 1, ("ab", "c"))]);
        var after = OrderApprovalFingerprint.Compute([Item("a", 1, ("a", "bc"))]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_RentalDatesChanged_DifferentHash()
    {
        var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var item = Item("a");
        item.RentalStartDateUtc = start;
        item.RentalEndDateUtc = start.AddDays(2);
        var before = OrderApprovalFingerprint.Compute([item]);

        item.RentalEndDateUtc = start.AddDays(3);
        var after = OrderApprovalFingerprint.Compute([item]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_EnteredPriceChanged_DifferentHash()
    {
        var item = Item("a");
        item.EnteredPrice = 10;
        var before = OrderApprovalFingerprint.Compute([item]);

        item.EnteredPrice = 5;
        var after = OrderApprovalFingerprint.Compute([item]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_WarehouseChanged_DifferentHash()
    {
        var item = Item("a");
        item.WarehouseId = "w1";
        var before = OrderApprovalFingerprint.Compute([item]);

        item.WarehouseId = "w2";
        var after = OrderApprovalFingerprint.Compute([item]);

        Assert.AreNotEqual(before, after);
    }

    [TestMethod]
    public void Compute_WishlistItemsIgnored_SameHash()
    {
        var wishlist = Item("w");
        wishlist.ShoppingCartTypeId = ShoppingCartType.Wishlist;

        var withoutWishlist = OrderApprovalFingerprint.Compute([Item("a")]);
        var withWishlist = OrderApprovalFingerprint.Compute([Item("a"), wishlist]);

        Assert.AreEqual(withoutWishlist, withWishlist);
    }

    [TestMethod]
    public void Compute_Called_ReturnsSha256Hex()
    {
        var hash = OrderApprovalFingerprint.Compute([Item("a")]);

        Assert.AreEqual(64, hash.Length);
        Assert.IsTrue(hash.All(Uri.IsHexDigit));
    }

    [TestMethod]
    public void Compute_EmptyCart_DifferentFromACart()
    {
        Assert.AreNotEqual(OrderApprovalFingerprint.Compute([]), OrderApprovalFingerprint.Compute([Item("a")]));
    }
}
