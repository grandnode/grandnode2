﻿using Grand.Domain.Payments;

namespace Grand.Business.Core.Utilities.Checkout
{
    /// <summary>
    /// Process payment result
    /// </summary>
    public class ProcessPaymentResult
    {
        private TransactionStatus _newPaymentTransactionStatus = TransactionStatus.Pending;

        /// <summary>
        /// Ctor
        /// </summary>
        public ProcessPaymentResult() 
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
        /// Gets or sets a payment transaction status after processing
        /// </summary>
        public TransactionStatus NewPaymentTransactionStatus
        {
            get => _newPaymentTransactionStatus;
            set => _newPaymentTransactionStatus = value;
        }

        /// <summary>
        /// Gets or sets value paid amount
        /// </summary>
        public double PaidAmount { get; set; }
    }
}
