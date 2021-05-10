namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a collection layout
    /// </summary>
    public partial class CollectionLayout : BaseEntity
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
