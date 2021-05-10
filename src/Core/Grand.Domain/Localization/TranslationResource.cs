namespace Grand.Domain.Localization
{
    /// <summary>
    /// Represents a locale string resource
    /// </summary>
    public partial class TranslationResource : BaseEntity
    {
        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resource value
        /// </summary>
        public string Value { get; set; }
        
    }

}
