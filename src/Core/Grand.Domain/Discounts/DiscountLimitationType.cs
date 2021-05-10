namespace Grand.Domain.Discounts
{
    /// <summary>
    /// Represents a discount limitation type
    /// </summary>
    public enum DiscountLimitationType
    {
        /// <summary>
        /// None
        /// </summary>
        Nolimits = 0,
        /// <summary>
        /// N Times Only
        /// </summary>
        NTimes = 1,
        /// <summary>
        /// N Times Per user
        /// </summary>
        NTimesPerUser = 2,
    }
}
