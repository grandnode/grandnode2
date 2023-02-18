﻿using Grand.Domain.Payments;

namespace Grand.Business.Core.Utilities.Checkout
{
    /// <summary>
    /// Refund payment result
    /// </summary>
    public class RefundPaymentResult
    {
        private TransactionStatus _newTransactionStatus = TransactionStatus.Pending;

        /// <summary>
        /// Ctor
        /// </summary>
        public RefundPaymentResult()
        {
            this.Errors = new List<string>();
        }

        /// <summary>
        /// Gets a value indicating whether request has been completed successfully
        /// </summary>
        public bool Success => this.Errors.Count == 0;

        /// <summary>
        /// Add error
        /// </summary>
        /// <param name="error">Error</param>
        public void AddError(string error)
        {
            this.Errors.Add(error);
        }

        /// <summary>
        /// Errors
        /// </summary>
        public IList<string> Errors { get; set; }

        /// <summary>
        /// Gets or sets a transaction status after processing
        /// </summary>
        public TransactionStatus NewTransactionStatus
        {
            get => _newTransactionStatus;
            set => _newTransactionStatus = value;
        }
    }
}
