using Grand.Domain.Audit;

namespace Grand.Domain.Payments;

/// <summary>
///     One money operation on a payment transaction, kept in its history. A record of its own, written once and never
///     changed, so a save of the transaction from a stale copy cannot drop it
/// </summary>
public class PaymentTransactionEntry : BaseEntity
{
    [AuditReference(typeof(PaymentTransaction))]
    [AuditOwner]
    public string PaymentTransactionId { get; set; } = null!;

    public PaymentTransactionEntryType EntryType { get; set; }

    public double Amount { get; set; }

    /// <summary>
    ///     Gets or sets who recorded the operation: a staff e-mail or a payment method system name
    /// </summary>
    public string? RecordedBy { get; set; }

    /// <summary>
    ///     Gets or sets the bank transfer title, receipt or gateway number the money can be matched with
    /// </summary>
    public string? Reference { get; set; }

    public string? Note { get; set; }
}
