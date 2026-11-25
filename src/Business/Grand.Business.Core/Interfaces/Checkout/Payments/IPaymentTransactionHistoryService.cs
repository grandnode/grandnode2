using Grand.Domain.Orders;
using Grand.Domain.Payments;

namespace Grand.Business.Core.Interfaces.Checkout.Payments;

/// <summary>
///     The money operations recorded on a payment transaction: what came in, what was authorized, captured, refunded
///     or voided, by whom and against which reference
/// </summary>
public interface IPaymentTransactionHistoryService
{
    /// <summary>
    ///     Records a money operation on the transaction, and the same operation as an order note
    /// </summary>
    /// <param name="paymentTransaction">The transaction the money moved on</param>
    /// <param name="order">The order the note goes to; null records no note</param>
    /// <param name="entryType">The kind of operation</param>
    /// <param name="amount">The amount in the transaction's currency</param>
    /// <param name="recordedBy">A staff e-mail or a payment method system name</param>
    /// <param name="reference">The bank transfer title, receipt or gateway number the money can be matched with</param>
    /// <param name="note">Anything else the staff want to keep with it</param>
    Task<PaymentTransactionEntry> Record(PaymentTransaction paymentTransaction, Order order,
        PaymentTransactionEntryType entryType, double amount, string recordedBy, string? reference = null,
        string? note = null);

    /// <summary>
    ///     Gets the operations recorded on the transaction, oldest first
    /// </summary>
    Task<IList<PaymentTransactionEntry>> GetEntries(string paymentTransactionId);
}
