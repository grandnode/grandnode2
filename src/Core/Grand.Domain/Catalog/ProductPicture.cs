namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product picture mapping
    /// </summary>
    public partial class ProductPicture : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the picture identifier
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
        
    }

}
