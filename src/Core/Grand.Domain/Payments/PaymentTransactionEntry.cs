namespace Grand.Domain.Payments;

/// <summary>
///     One money operation on a payment transaction, kept in its history
/// </summary>
public class PaymentTransactionEntry
{
    public PaymentTransactionEntryType EntryType { get; set; }

    public double Amount { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets who recorded the operation: a staff e-mail or a payment method system name
    /// </summary>
    public string CreatedBy { get; set; }

    /// <summary>
    ///     Gets or sets the bank transfer title, receipt or gateway number the money can be matched with
    /// </summary>
    public string Reference { get; set; }

    public string Note { get; set; }
}
