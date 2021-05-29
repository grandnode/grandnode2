using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class GiftVoucherModel : BaseEntityModel
    {
        public GiftVoucherModel()
        {
            AvailableCurrencies = new List<SelectListItem>();
        }
        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.GiftVoucherType")]
        public int GiftVoucherTypeId { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.Order")]
        public string PurchasedWithOrderId { get; set; }
        public int PurchasedWithOrderNumber { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.Amount")]
        public double Amount { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }


        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.Amount")]
        public string AmountStr { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.RemainingAmount")]
        public string RemainingAmountStr { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.IsGiftVoucherActivated")]
        public bool IsGiftVoucherActivated { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.Code")]

        public string Code { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.RecipientName")]

        public string RecipientName { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.RecipientEmail")]

        public string RecipientEmail { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.SenderName")]

        public string SenderName { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.SenderEmail")]

        public string SenderEmail { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.Message")]

        public string Message { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.IsRecipientNotified")]
        public bool IsRecipientNotified { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.ValidTo")]
        [UIHint("DateTimeNullable")]
        public DateTime? ValidTo { get; set; }

        [GrandResourceDisplayName("Admin.GiftVouchers.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        #region Nested classes

        public partial class GiftVoucherUsageHistoryModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.GiftVouchers.History.UsedValue")]
            public string UsedValue { get; set; }

            [GrandResourceDisplayName("Admin.GiftVouchers.History.Order")]
            public string OrderId { get; set; }
            public int OrderNumber { get; set; }

            [GrandResourceDisplayName("Admin.GiftVouchers.History.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        #endregion
    }
}