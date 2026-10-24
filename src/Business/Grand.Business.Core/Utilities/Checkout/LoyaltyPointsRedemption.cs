namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Loyalty points redeemed on an order total
/// </summary>
/// <param name="Points">Loyalty points redeemed</param>
/// <param name="Amount">Amount the redeemed loyalty points cover, in the context currency</param>
public sealed record LoyaltyPointsRedemption(int Points, double Amount);
