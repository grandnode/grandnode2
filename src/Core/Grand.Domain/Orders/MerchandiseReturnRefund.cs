using Grand.Domain.Audit;
using Grand.Domain.Payments;

namespace Grand.Domain.Orders;

/// <summary>
///     The money given back for a merchandise return through the payment of its order; the amounts are in the order's
///     currency
/// </summary>
public class MerchandiseReturnRefund
{
    /// <summary>
    ///     Gets or sets what was paid for the items of the return: their price with the item discounts and the tax
    /// </summary>
    public double ItemsAmount { get; set; }

    /// <summary>
    ///     Gets or sets what the customer pays to ship the return, deducted from the items
    /// </summary>
    public double ReturnShippingCost { get; set; }

    /// <summary>
    ///     Gets or sets the amount refunded: the items less the return shipping cost, or less when the staff lowered it
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    ///     Gets or sets whether the money was given back outside the payment provider, e.g. by a bank transfer
    /// </summary>
    public bool Offline { get; set; }

    /// <summary>
    ///     Gets or sets the payment transaction refunded
    /// </summary>
    [AuditReference(typeof(PaymentTransaction))]
    public string PaymentTransactionId { get; set; }

    /// <summary>
    ///     Gets or sets the refunded amount of the payment transaction before this refund: whether the refund went
    ///     through is read from the transaction when its result was not saved
    /// </summary>
    public double TransactionRefundedAmount { get; set; }

    /// <summary>
    ///     Gets or sets who refunded, as the staff note names them
    /// </summary>
    public string RefundedBy { get; set; }

    /// <summary>
    ///     Gets or sets when the refund was started
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets when the money was given back; null while the refund is under way
    /// </summary>
    public DateTime? RefundedOnUtc { get; set; }
}
