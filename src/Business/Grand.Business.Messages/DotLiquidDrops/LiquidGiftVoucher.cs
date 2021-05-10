using DotLiquid;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidGiftVoucher : Drop
    {
        private GiftVoucher _giftVoucher;

        public LiquidGiftVoucher(GiftVoucher giftVoucher)
        {
            _giftVoucher = giftVoucher;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string SenderName
        {
            get { return _giftVoucher.SenderName; }
        }

        public string SenderEmail
        {
            get { return _giftVoucher.SenderEmail; }
        }

        public string RecipientName
        {
            get { return _giftVoucher.RecipientName; }
        }

        public string RecipientEmail
        {
            get { return _giftVoucher.RecipientEmail; }
        }

        public string Amount { get; set; }

        public string CouponCode
        {
            get { return _giftVoucher.Code; }
        }

        public string Message
        {
            get
            {
                var giftVoucherMesage = !String.IsNullOrWhiteSpace(_giftVoucher.Message) ? FormatText.ConvertText(_giftVoucher.Message) : "";
                return giftVoucherMesage;
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}