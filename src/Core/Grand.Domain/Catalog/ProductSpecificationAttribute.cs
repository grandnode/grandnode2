namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product specification attribute
    /// </summary>
    public partial class ProductSpecificationAttribute : SubBaseEntity
    {       

        /// <summary>
        /// Gets or sets the attribute type ID
        /// </summary>
        public SpecificationAttributeType AttributeTypeId { get; set; }

        /// <summary>
        /// Gets or sets the specification attribute Id
        /// </summary>
        public string SpecificationAttributeId { get; set; }

        /// <summary>
        /// Gets or sets the specification attribute identifier
        /// </summary>
        public string SpecificationAttributeOptionId { get; set; }

        /// <summary>
        /// Gets or sets the custom name
        /// </summary>
        public string CustomName { get; set; }

        /// <summary>
        /// Gets or sets the custom value
        /// </summary>
        public string CustomValue { get; set; }

        /// <summary>
        /// Gets or sets whether the attribute can be filtered by
        /// </summary>
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets whether the attribute will be shown on the product page
        /// </summary>
        public bool ShowOnProductPage { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
        
    }
}
