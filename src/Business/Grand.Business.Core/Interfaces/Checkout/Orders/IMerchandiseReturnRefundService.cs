using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Gives back the money for a merchandise return through the payment of its order, with the partial refund
///     commands of the payment transaction. A return is refunded once, after its goods are received; the refunds of an
///     order's returns are made one at a time, each saved on its return before the payment is asked. A refund the
///     payment answered with errors, or one whose call ended without saving its result, waits for the staff to say
///     whether the money went back
/// </summary>
public interface IMerchandiseReturnRefundService
{
    /// <summary>
    ///     What the refund of the return comes to now, shown to the staff before they refund. The amounts are filled for
    ///     a return not received yet as well, with the error RefundNotAllowed: the customer is shown them as an estimate
    ///     before the return is submitted. A return refunded or being refunded gets its error alone
    /// </summary>
    /// <param name="merchandiseReturn">The return</param>
    /// <param name="order">The order of the return</param>
    Task<MerchandiseReturnRefundQuote> GetQuote(MerchandiseReturn merchandiseReturn, Order order);

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

    /// <summary>
    ///     Records what the staff found when they checked the payment of a refund that waits for it: the money went
    ///     back, and the return is refunded and completed; or it did not, and the return can be refunded again
    /// </summary>
    /// <param name="merchandiseReturnId">The return</param>
    /// <param name="wentBack">Whether the money went back</param>
    /// <param name="checkedBy">Who checked, as the staff notes name them; null for the system</param>
    /// <returns>null when recorded, else a key of MerchandiseReturnErrors</returns>
    Task<string> ResolveRefund(string merchandiseReturnId, bool wentBack, string checkedBy = null);
}
