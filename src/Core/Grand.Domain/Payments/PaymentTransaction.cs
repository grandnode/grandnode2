using System;
using System.Collections.Generic;

namespace Grand.Domain.Payments
{
    public class PaymentTransaction : BaseEntity
    {
        public PaymentTransaction()
        {
            CustomValues = new Dictionary<string, object>();
            Errors = new List<string>();
        }

        /// <summary>
        /// Gets or sets payment method system name
        /// </summary>
        public string PaymentMethodSystemName { get; set; }

        /// <summary>
        /// <summary>
        /// Gets or sets transaction status
        /// </summary>
        public TransactionStatus TransactionStatus { get; set; }
        /// <summary>
        /// Gets or sets store ident
        /// </summary>
        public string StoreId { get; set; }
        /// <summary>
        /// Gets or sets order guid
        /// </summary>
        public Guid OrderGuid { get; set; }

        /// <summary>
        /// Gets or sets order code
        /// </summary>
        public string OrderCode { get; set; }
        /// <summary>
        /// Gets or sets customer ident
        /// </summary>
        public string CustomerId { get; set; }
        /// <summary>
        /// Gets or sets customer email
        /// </summary>
        public string CustomerEmail { get; set; }
        /// <summary>
        /// Gets or sets currency code
        /// </summary>
        public string CurrencyCode { get; set; }

        public double CurrencyRate { get; set; }

        /// <summary>
        /// Gets or sets amount 
        /// </summary>
        public double TransactionAmount { get; set; }

        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets the refunded amount
        /// </summary>
        public double RefundedAmount { get; set; }

        /// <summary>
        /// Gets or sets id transaction
        /// </summary>
        public string IPAddress { get; set; }
        /// <summary>
        /// Gets or sets authorization transaction id
        /// </summary>
        public string AuthorizationTransactionId { get; set; }
        /// <summary>
        /// Gets or sets authorization transaction code
        /// </summary>
        public string AuthorizationTransactionCode { get; set; }
        /// <summary>
        /// Gets or sets authorization transaction result
        /// </summary>
        public string AuthorizationTransactionResult { get; set; }
        /// <summary>
        /// Gets or sets capture transaction id
        /// </summary>
        public string CaptureTransactionId { get; set; }
        /// <summary>
        /// Gets or sets capture transaction result
        /// </summary>
        public string CaptureTransactionResult { get; set; }
        /// <summary>
        /// Gets or sets description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets additional info
        /// </summary>
        public string AdditionalInfo { get; set; }

        /// <summary>
        /// Gets or sets custom values
        /// </summary>
        public Dictionary<string, object> CustomValues { get; set; }

        /// <summary>
        /// gets or sets errors
        /// </summary>
        public IList<string> Errors { get; set; }

        /// <summary>
        /// Gets or sets the date and time of transaction creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
        /// <summary>
        /// Gets or sets the date and time of transaction updated
        /// </summary>
        public DateTime? UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets
        /// </summary>
        public bool Temp { get; set; }
    }
}
