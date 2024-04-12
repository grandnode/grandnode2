using DotLiquid;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidGiftVoucher : Drop
{
    private readonly GiftVoucher _giftVoucher;

    public LiquidGiftVoucher(GiftVoucher giftVoucher)
    {
        _giftVoucher = giftVoucher;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string SenderName => _giftVoucher.SenderName;

    public string SenderEmail => _giftVoucher.SenderEmail;

    public string RecipientName => _giftVoucher.RecipientName;

    public string RecipientEmail => _giftVoucher.RecipientEmail;

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