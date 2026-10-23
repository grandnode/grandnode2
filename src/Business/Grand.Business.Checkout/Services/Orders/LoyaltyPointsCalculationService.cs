using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Loyalty points calculation
/// </summary>
public class LoyaltyPointsCalculationService : ILoyaltyPointsCalculationService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="loyaltyPointsService">Loyalty points service</param>
    /// <param name="currencyService">Currency service</param>
    /// <param name="loyaltyPointsSettings">Loyalty points settings</param>
    /// <param name="shoppingCartSettings">Shopping cart settings</param>
    public LoyaltyPointsCalculationService(ILoyaltyPointsService loyaltyPointsService,
        ICurrencyService currencyService,
        LoyaltyPointsSettings loyaltyPointsSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _loyaltyPointsService = loyaltyPointsService;
        _currencyService = currencyService;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly ICurrencyService _currencyService;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Converts existing loyalty points to amount
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points</param>
    /// <returns>Converted value</returns>
    public virtual async Task<double> ConvertLoyaltyPointsToAmount(int loyaltyPoints)
    {
        if (loyaltyPoints <= 0)
            return 0;

        //the amount is in the primary store currency, so it is rounded in that currency
        var result = loyaltyPoints * _loyaltyPointsSettings.ExchangeRate;
        if (_shoppingCartSettings.RoundPrices)
            result = RoundingHelper.RoundPrice(result, await _currencyService.GetPrimaryStoreCurrency());

        return result;
    }

    /// <summary>
    ///     Converts an amount to loyalty points
    /// </summary>
    /// <param name="amount">Amount</param>
    /// <returns>Converted value</returns>
    public virtual int ConvertAmountToLoyaltyPoints(double amount)
    {
        var result = 0;
        if (amount <= 0)
            return 0;

        if (_loyaltyPointsSettings.ExchangeRate > 0)
            result = (int)Math.Ceiling(amount / _loyaltyPointsSettings.ExchangeRate);
        return result;
    }

    /// <summary>
    ///     Gets a value indicating whether a customer has minimum amount of loyalty points to use (if enabled)
    /// </summary>
    /// <param name="loyaltyPoints">Loyalty points to check</param>
    /// <returns>true - loyalty points could use; false - cannot be used.</returns>
    public virtual bool CheckMinimumLoyaltyPointsToUseRequirement(int loyaltyPoints)
    {
        if (_loyaltyPointsSettings.MinimumLoyaltyPointsToUse <= 0)
            return true;

        return loyaltyPoints >= _loyaltyPointsSettings.MinimumLoyaltyPointsToUse;
    }

    /// <summary>
    ///     Gets the loyalty points the customer redeems on an order total
    /// </summary>
    /// <param name="context">Customer, store, currency and tax display the calculation is for</param>
    /// <param name="orderTotal">Order total the points are redeemed on, in the context currency</param>
    /// <param name="useLoyaltyPoints">Use Loyalty Points; null to read the choice of the customer</param>
    /// <returns>Redeemed points and the amount they cover</returns>
    public virtual async Task<LoyaltyPointsRedemption> GetLoyaltyPointsRedemption(CalculationContext context,
        double orderTotal, bool? useLoyaltyPoints)
    {
        var redeemedLoyaltyPoints = 0;
        double redeemedLoyaltyPointsAmount = 0;

        if (_loyaltyPointsSettings.Enabled)
        {
            useLoyaltyPoints ??= context.Customer.GetUserFieldFromEntity<bool>(
                SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout,
                context.Store.Id);

            if (useLoyaltyPoints.Value)
            {
                var loyaltyPointsBalance =
                    await _loyaltyPointsService.GetLoyaltyPointsBalance(context.Customer.Id,
                        context.Store.Id);
                if (CheckMinimumLoyaltyPointsToUseRequirement(loyaltyPointsBalance))
                {
                    var loyaltyPointsBalanceAmount = await ConvertLoyaltyPointsToAmount(loyaltyPointsBalance);
                    if (orderTotal > 0)
                    {
                        if (orderTotal > loyaltyPointsBalanceAmount)
                        {
                            redeemedLoyaltyPoints = loyaltyPointsBalance;
                            redeemedLoyaltyPointsAmount =
                                await _currencyService.ConvertFromPrimaryStoreCurrency(loyaltyPointsBalanceAmount,
                                    context.Currency);
                        }
                        else
                        {
                            redeemedLoyaltyPointsAmount = orderTotal;
                            redeemedLoyaltyPoints = ConvertAmountToLoyaltyPoints(
                                await _currencyService.ConvertToPrimaryStoreCurrency(redeemedLoyaltyPointsAmount,
                                    context.Currency));
                        }
                    }
                }
            }
        }

        return new LoyaltyPointsRedemption(redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount);
    }

    #endregion
}
