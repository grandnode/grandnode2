namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Whether the order being placed needs an approval before it is released
/// </summary>
/// <param name="Required">The customer is a limited buyer and the order total exceeds the limit</param>
/// <param name="Limit">The customer's order limit in the primary store currency; null when there is none</param>
/// <param name="Total">The order total in the primary store currency; null when it is not known yet</param>
public record OrderApprovalCheckoutState(bool Required, double? Limit, double? Total);
