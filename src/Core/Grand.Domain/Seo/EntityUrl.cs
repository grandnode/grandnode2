namespace Grand.Domain.Seo
{
    /// <summary>
    /// Represents an URL
    /// </summary>
    public partial class EntityUrl : BaseEntity
    {
        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity name
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the slug
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the item is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public string LanguageId { get; set; }
    }
}
