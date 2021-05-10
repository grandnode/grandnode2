namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product layout
    /// </summary>
    public partial class ProductLayout : BaseEntity
    {
        /// <summary>
        /// Gets or sets the layout name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the view path
        /// </summary>
        public string ViewPath { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
