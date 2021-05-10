namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product type
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Simple
        /// </summary>
        SimpleProduct = 0,
        /// <summary>
        /// Grouped product
        /// </summary>
        GroupedProduct = 10,

        /// <summary>
        /// Reservation product
        /// </summary>
        Reservation = 20,

        /// <summary>
		/// Bundled product
		/// </summary>
		BundledProduct = 30,

        /// <summary>
        /// Auction product
        /// </summary>
        Auction = 40
    }
}
