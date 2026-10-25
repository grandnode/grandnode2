using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Whether a cart needs an order approval before the order can be placed, and where that approval stands
/// </summary>
/// <param name="Required">The customer is a limited buyer and the order total exceeds the limit</param>
/// <param name="Limit">The customer's order limit in the primary store currency; null when there is none</param>
/// <param name="Total">The order total in the primary store currency; null when it is not known yet</param>
/// <param name="Active">
///     The approval covering the cart when there is one, otherwise the customer's pending request; null when neither
///     exists
/// </param>
public record OrderApprovalCheckoutState(bool Required, double? Limit, double? Total, OrderApproval Active)
{
    /// <summary>
    ///     Gets a value indicating whether the order must not be placed yet
    /// </summary>
    public bool Blocked => Required && Active?.Status != OrderApprovalStatus.Approved;
}
