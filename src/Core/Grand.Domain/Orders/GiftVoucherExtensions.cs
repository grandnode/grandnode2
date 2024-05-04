using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Stores;

namespace Grand.Domain.Orders;

/// <summary>
///     Extensions
/// </summary>
public static class GiftVoucherExtensions
{
    /// <summary>
    ///     Add gift voucher attrbibutes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="recipientName">Recipient name</param>
    /// <param name="recipientEmail">Recipient email</param>
    /// <param name="senderName">Sender name</param>
    /// <param name="senderEmail">Sender email</param>
    /// <param name="giftVoucherMessage">Message</param>
    /// <returns>Attributes</returns>
    public static IList<CustomAttribute> AddGiftVoucherAttribute(IList<CustomAttribute> customAttributes,
        string recipientName,
        string recipientEmail, string senderName, string senderEmail, string giftVoucherMessage)
    {
        if (customAttributes == null)
            customAttributes = new List<CustomAttribute>();

        customAttributes.Add(new CustomAttribute { Key = "RecipientName", Value = recipientName });
        customAttributes.Add(new CustomAttribute { Key = "RecipientEmail", Value = recipientEmail });
        customAttributes.Add(new CustomAttribute { Key = "SenderName", Value = senderName });
        customAttributes.Add(new CustomAttribute { Key = "SenderEmail", Value = senderEmail });
        customAttributes.Add(new CustomAttribute { Key = "Message", Value = giftVoucherMessage });

        return customAttributes;
    }

    /// <summary>
    ///     Get gift voucher attrbibutes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="recipientName">Recipient name</param>
    /// <param name="recipientEmail">Recipient email</param>
    /// <param name="senderName">Sender name</param>
    /// <param name="senderEmail">Sender email</param>
    /// <param name="giftVoucherMessage">Message</param>
    public static void GetGiftVoucherAttribute(IList<CustomAttribute> customAttributes, out string recipientName,
        out string recipientEmail, out string senderName,
        out string senderEmail, out string giftVoucherMessage)
    {
        if (customAttributes == null)
            customAttributes = new List<CustomAttribute>();

        recipientName = customAttributes.FirstOrDefault(x => x.Key == "RecipientName")?.Value;
        recipientEmail = customAttributes.FirstOrDefault(x => x.Key == "RecipientEmail")?.Value;
        senderName = customAttributes.FirstOrDefault(x => x.Key == "SenderName")?.Value;
        senderEmail = customAttributes.FirstOrDefault(x => x.Key == "SenderEmail")?.Value;
        giftVoucherMessage = customAttributes.FirstOrDefault(x => x.Key == "Message")?.Value;
    }

    /// <summary>
    ///     Gets a gift voucher remaining amount
    /// </summary>
    /// <returns>Gift voucher remaining amount</returns>
    public static double GetGiftVoucherRemainingAmount(this GiftVoucher giftVoucher)
    {
        var result = giftVoucher.Amount;

        foreach (var gcuh in giftVoucher.GiftVoucherUsageHistory)
            result -= gcuh.UsedValue;

        if (result < 0)
            result = 0;

        return result;
    }

    /// <summary>
    ///     Is gift voucher valid
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    /// <param name="currency">Currency</param>
    /// <param name="store">Store</param>
    /// <returns>Result</returns>
    public static bool IsGiftVoucherValid(this GiftVoucher giftVoucher, Currency currency, Store store)
    {
        if (!giftVoucher.IsGiftVoucherActivated)
            return false;

        if (giftVoucher.CurrencyCode != currency.CurrencyCode)
            return false;

        if (giftVoucher.ValidTo.HasValue && giftVoucher.ValidTo.Value <= DateTime.UtcNow)
            return false;

        if (!string.IsNullOrEmpty(giftVoucher.StoreId) && giftVoucher.StoreId != store.Id)
            return false;

        var remainingAmount = giftVoucher.GetGiftVoucherRemainingAmount();
        if (remainingAmount > 0)
            return true;

        return false;
    }
}