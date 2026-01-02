using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Orders;

[TestClass]
public class GiftVoucherExtensionsTests
{
    private readonly GiftVoucher giftVoucher = new() {
        Amount = 10,
        Code = "123456",
        CurrencyCode = "USD",
        IsGiftVoucherActivated = true
    };

    [TestMethod]
    public void AddGiftVoucherAttributeTest()
    {
        IList<CustomAttribute> customAttributes = new List<CustomAttribute>();
        GiftVoucherExtensions.AddGiftVoucherAttribute(customAttributes, "Johny", "test@test.com", "My name",
            "name@name.com", "my sample message");

        GiftVoucherExtensions.GetGiftVoucherAttribute(customAttributes, out var giftVoucherRecipientName,
            out var giftVoucherRecipientEmail,
            out var giftVoucherSenderName, out var giftVoucherSenderEmail, out var giftVoucherMessage);

        Assert.AreEqual("Johny", giftVoucherRecipientName);
        Assert.AreEqual("test@test.com", giftVoucherRecipientEmail);
        Assert.AreEqual("My name", giftVoucherSenderName);
        Assert.AreEqual("name@name.com", giftVoucherSenderEmail);
        Assert.AreEqual("my sample message", giftVoucherMessage);
    }

    [TestMethod]
    public void GetGiftVoucherAttributeTest()
    {
        IList<CustomAttribute> customAttributes = new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = "RecipientName", Value = "Johny" });
        customAttributes.Add(new CustomAttribute { Key = "RecipientEmail", Value = "test@test.com" });
        customAttributes.Add(new CustomAttribute { Key = "SenderName", Value = "My name" });
        customAttributes.Add(new CustomAttribute { Key = "SenderEmail", Value = "name@name.com" });
        customAttributes.Add(new CustomAttribute { Key = "Message", Value = "my sample message" });
        customAttributes.Add(new CustomAttribute { Key = "Test", Value = "my test" });

        GiftVoucherExtensions.GetGiftVoucherAttribute(customAttributes, out var giftVoucherRecipientName,
            out var giftVoucherRecipientEmail,
            out var giftVoucherSenderName, out var giftVoucherSenderEmail, out var giftVoucherMessage);

        Assert.AreEqual("Johny", giftVoucherRecipientName);
        Assert.AreEqual("test@test.com", giftVoucherRecipientEmail);
        Assert.AreEqual("My name", giftVoucherSenderName);
        Assert.AreEqual("name@name.com", giftVoucherSenderEmail);
        Assert.AreEqual("my sample message", giftVoucherMessage);
    }

    [TestMethod]
    public void GetGiftVoucherRemainingAmountTest()
    {
        giftVoucher.GiftVoucherUsageHistory.Add(new GiftVoucherUsageHistory { UsedValue = 3 });
        giftVoucher.GiftVoucherUsageHistory.Add(new GiftVoucherUsageHistory { UsedValue = 4 });
        Assert.AreEqual(3, giftVoucher.GetGiftVoucherRemainingAmount());
    }

    [TestMethod]
    public void IsGiftVoucherValidTest_ValidTo_True()
    {
        giftVoucher.ValidTo = DateTime.UtcNow.AddDays(1);
        Assert.IsTrue(giftVoucher.IsGiftVoucherValid(new Currency { CurrencyCode = "USD" }, new Store()));
    }

    [TestMethod]
    public void IsGiftVoucherValidTest_False()
    {
        giftVoucher.ValidTo = DateTime.UtcNow.AddDays(-1);
        Assert.IsFalse(giftVoucher.IsGiftVoucherValid(new Currency { CurrencyCode = "USD" }, new Store()));
    }
}