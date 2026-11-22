using DotLiquid;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidGiftVoucher : Drop
{
    private readonly GiftVoucher _giftVoucher;

    public LiquidGiftVoucher(GiftVoucher giftVoucher)
    {
        _giftVoucher = giftVoucher;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string SenderName => WebUtility.HtmlEncode(_giftVoucher.SenderName);

    public string SenderEmail => WebUtility.HtmlEncode(_giftVoucher.SenderEmail);

    public string RecipientName => WebUtility.HtmlEncode(_giftVoucher.RecipientName);

    public string RecipientEmail => WebUtility.HtmlEncode(_giftVoucher.RecipientEmail);

    public string Amount { get; set; }

    public string CouponCode => _giftVoucher.Code;

    public string Message {
        get {
            var giftVoucherMesage = !string.IsNullOrWhiteSpace(_giftVoucher.Message)
                ? FormatText.ConvertText(_giftVoucher.Message)
                : "";
            return giftVoucherMesage;
        }
    }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}