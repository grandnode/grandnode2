namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Manage product combination tier prices
    /// </summary>
    public partial class ProductCombinationTierPrices : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the customer group identifier
        /// </summary>
        public string CustomerGroupId { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the price
        /// </summary>
        public double Price { get; set; }

    }
}
