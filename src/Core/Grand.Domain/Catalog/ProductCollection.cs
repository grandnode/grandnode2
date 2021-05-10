namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product collection mapping
    /// </summary>
    public partial class ProductCollection : SubBaseEntity
    {
        
        /// <summary>
        /// Gets or sets the collection identifier
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the product is featured
        /// </summary>
        public bool IsFeaturedProduct { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

    }

}
