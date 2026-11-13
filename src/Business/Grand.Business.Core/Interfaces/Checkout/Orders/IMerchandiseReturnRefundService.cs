using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Gives back the money for a merchandise return through the payment of its order, with the partial refund
///     commands of the payment transaction. A return is refunded once, after its goods are received; the refunds of an
///     order's returns are made one at a time, each saved on its return before the payment is asked
/// </summary>
public interface IMerchandiseReturnRefundService
{
    /// <summary>
    ///     What the refund of the return comes to now, shown to the staff before they refund; null when the return or
    ///     its order is not found
    /// </summary>
    Task<MerchandiseReturnRefundQuote> GetQuote(string merchandiseReturnId);

    /// <summary>
    ///     Refunds the amount and completes the return: through the payment provider, or offline when the money was
    ///     given back otherwise, e.g. by a bank transfer. The return records what was refunded and when, and the order
    ///     gets a note
    /// </summary>
    /// <param name="merchandiseReturnId">The return</param>
    /// <param name="amount">The amount, at most the quote's MaxAmount; the staff may lower it</param>
    /// <param name="offline">Whether the money was given back outside the payment provider</param>
    /// <param name="refundedBy">Who refunded, as the staff notes name them; null for the system</param>
    /// <returns>null when refunded, else a key of MerchandiseReturnErrors</returns>
    Task<string> Refund(string merchandiseReturnId, double amount, bool offline, string refundedBy = null);
}
