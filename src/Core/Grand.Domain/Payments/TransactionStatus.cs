namespace Grand.Domain.Payments
{
    public enum TransactionStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,
        /// <summary>
        /// Authorized
        /// </summary>
        Authorized = 10,
        /// <summary>
        /// Partially Paid
        /// </summary>
        PartialPaid = 15,
        /// <summary>
        /// Paid
        /// </summary>
        Paid = 20,
        /// <summary>
        /// Partially Refunded
        /// </summary>
        PartiallyRefunded = 25,
        /// <summary>
        /// Refunded
        /// </summary>
        Refunded = 30,
        /// <summary>
        /// Voided
        /// </summary>
        Voided = 40,
        /// <summary>
        /// Canceled
        /// </summary>
        Canceled = 50,
    }
}
