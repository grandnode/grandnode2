using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Converts loyalty points to amounts and back, and works out how many points an order total redeems
/// </summary>
public interface ILoyaltyPointsCalculationService
{
    /// <summary>
    ///     Converts existing loyalty points to amount
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points</param>
    /// <returns>Converted value in the primary store currency</returns>
    Task<double> ConvertLoyaltyPointsToAmount(int loyaltyPoints);

    /// <summary>
    ///     Converts an amount to loyalty points
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <returns>Converted value</returns>
    int ConvertAmountToLoyaltyPoints(double amount);

    /// <summary>
    ///     Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points to check</param>
    /// <returns>true - loyalty points could use; false - cannot be used.</returns>
    bool CheckMinimumLoyaltyPointsToUseRequirement(int loyaltyPoints);

    /// <summary>
    ///     Gets the loyalty points the customer redeems on an order total
    /// </summary>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="orderTotal">Order total the points are redeemed on, in the context currency</param>
    /// <param name="useLoyaltyPoints">
    ///     A value indicating loyalty points should be used; null to detect current choice of the
    ///     customer
    /// </param>
    /// <returns>Redeemed points and the amount they cover; zero when loyalty points are disabled or not used</returns>
    Task<LoyaltyPointsRedemption> GetLoyaltyPointsRedemption(CalculationContext context, double orderTotal,
        bool? useLoyaltyPoints);
}
