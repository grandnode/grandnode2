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

        Assert.IsTrue(giftVoucherRecipientName == "Johny");
        Assert.IsTrue(giftVoucherRecipientEmail == "test@test.com");
        Assert.IsTrue(giftVoucherSenderName == "My name");
        Assert.IsTrue(giftVoucherSenderEmail == "name@name.com");
        Assert.IsTrue(giftVoucherMessage == "my sample message");
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

        Assert.IsTrue(giftVoucherRecipientName == "Johny");
        Assert.IsTrue(giftVoucherRecipientEmail == "test@test.com");
        Assert.IsTrue(giftVoucherSenderName == "My name");
        Assert.IsTrue(giftVoucherSenderEmail == "name@name.com");
        Assert.IsTrue(giftVoucherMessage == "my sample message");
    }

    [TestMethod]
    public void GetGiftVoucherRemainingAmountTest()
    {
        giftVoucher.GiftVoucherUsageHistory.Add(new GiftVoucherUsageHistory { UsedValue = 3 });
        giftVoucher.GiftVoucherUsageHistory.Add(new GiftVoucherUsageHistory { UsedValue = 4 });
        Assert.IsTrue(giftVoucher.GetGiftVoucherRemainingAmount() == 3);
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