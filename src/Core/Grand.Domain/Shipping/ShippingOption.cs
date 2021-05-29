namespace Grand.Domain.Shipping
{
    /// <summary>
    /// Represents a shipping option
    /// </summary>
    public partial class ShippingOption
    {
        /// <summary>
        /// Gets or sets the system name of shipping rate provider
        /// </summary>
        public string ShippingRateProviderSystemName { get; set; }

        /// <summary>
        /// Gets or sets a shipping rate (without discounts, additional shipping charges, etc)
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Gets or sets a shipping option name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a shipping option description
        /// </summary>
        public string Description { get; set; }
    }

}
