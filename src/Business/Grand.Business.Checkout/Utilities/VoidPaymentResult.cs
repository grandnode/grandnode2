using Grand.Domain.Payments;
using System.Collections.Generic;

namespace Grand.Business.Checkout.Utilities
{
    /// <summary>
    /// Represents a VoidPaymentResult
    /// </summary>
    public partial class VoidPaymentResult
    {
        private TransactionStatus _newTransactionStatus = TransactionStatus.Pending;

        /// <summary>
        /// Ctor
        /// </summary>
        public VoidPaymentResult()
        {
            Errors = new List<string>();
        }

        /// <summary>
        /// Gets a value indicating whether request has been completed successfully
        /// </summary>
        public bool Success
        {
            get { return (Errors.Count == 0); }
        }

        /// <summary>
        /// Add error
        /// </summary>
        /// <param name="error">Error</param>
        public void AddError(string error)
        {
            Errors.Add(error);
        }

        /// <summary>
        /// Errors
        /// </summary>
        public IList<string> Errors { get; set; }

        /// <summary>
        /// Gets or sets a payment transaction status after processing
        /// </summary>
        public TransactionStatus NewTransactionStatus
        {
            get
            {
                return _newTransactionStatus;
            }
            set
            {
                _newTransactionStatus = value;
            }
        }
    }
}
