using Grand.Domain.Configuration;

namespace Grand.Domain.Orders
{
    public class LoyaltyPointsSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether Loyalty Points Program is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value of Loyalty Points exchange rate
        /// </summary>
        public double ExchangeRate { get; set; }

        /// <summary>
        /// Gets or sets the minimum loyalty points to use
        /// </summary>
        public int MinimumLoyaltyPointsToUse { get; set; }

        /// <summary>
        /// Gets or sets a number of points awarded for registration
        /// </summary>
        public int PointsForRegistration { get; set; }

        /// <summary>
        /// Gets or sets a number of points awarded for purchases (amount in primary store currency)
        /// </summary>
        public double PointsForPurchases_Amount { get; set; }

        /// <summary>
        /// Gets or sets a number of points awarded for purchases
        /// </summary>
        public int PointsForPurchases_Points { get; set; }

        /// <summary>
        /// Points are awarded when the order status is
        /// </summary>
        public int PointsForPurchases_Awarded { get; set; }

        /// <summary>
        /// Loyalty points are canceled when the order is canceled
        /// </summary>
        public bool ReduceLoyaltyPointsAfterCancelOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether "You will earn" message should be displayed
        /// </summary>
        public bool DisplayHowMuchWillBeEarned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all loyalty points are accumulated in one balance for all stores and they can be used in any store. Otherwise, each store has its own loyalty points and they can only be used in that store.
        /// </summary>
        public bool PointsAccumulatedForAllStores { get; set; }
    }
}