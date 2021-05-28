using Grand.Domain.Payments;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class PaymentTransactionModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PaymentMethodSystemName")]
        public string PaymentMethodSystemName { get; set; }

        public TransactionStatus TransactionStatus { get; set; }
        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.Status")]
        public string Status { get; set; }

        public string StoreId { get; set; }
        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.Store")]
        public string StoreName { get; set; }

        public Guid OrderGuid { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.Order")]
        public string OrderId { get; set; }
        public int? OrderNumber { get; set; }
        public string OrderCode { get; set; }

        public string CustomerId { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.Customer")]
        public string CustomerEmail { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.CurrencyRate")]
        public double CurrencyRate { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.TransactionAmount")]
        public double TransactionAmount { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PaidAmount")]
        public double PaidAmount { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.RefundedAmount")]
        public double RefundedAmount { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.IPAddress")]
        public string IPAddress { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.AuthorizationTransactionId")]
        public string AuthorizationTransactionId { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.AuthorizationTransactionCode")]
        public string AuthorizationTransactionCode { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.AuthorizationTransactionResult")]
        public string AuthorizationTransactionResult { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.CaptureTransactionId")]
        public string CaptureTransactionId { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.CaptureTransactionResult")]
        public string CaptureTransactionResult { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.AdditionalInfo")]
        public string AdditionalInfo { get; set; }
        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PartialRefund.MaxAmountToRefund")]
        public double MaxAmountToRefund { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PartialRefund.MaxAmountToPaid")]
        public double MaxAmountToPaid { get; set; }

        public bool CanCapture { get; set; }
        public bool CanMarkAsPaid { get; set; }
        public bool CanRefund { get; set; }
        public bool CanRefundOffline { get; set; }
        public bool CanPartiallyRefund { get; set; }
        public bool CanPartiallyRefundOffline { get; set; }
        public bool CanPartiallyPaidOffline { get; set; }
        public bool CanVoid { get; set; }
        public bool CanVoidOffline { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PartialRefund.AmountToRefund")]
        public double AmountToRefund { get; set; }

        [GrandResourceDisplayName("Admin.PaymentTransaction.Fields.PartialRefund.AmountToPaid")]
        public double AmountToPaid { get; set; }
    }
}
