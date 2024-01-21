namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product review helpfulness
    /// </summary>
    public class ProductReviewHelpfulness : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the product review identifier
        /// </summary>
        public string ProductReviewId { get; set; }

        /// <summary>
        /// A value indicating whether a review a helpful
        /// </summary>
        public bool WasHelpful { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

    }
}
