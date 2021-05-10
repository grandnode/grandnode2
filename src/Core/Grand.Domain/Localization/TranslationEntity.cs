namespace Grand.Domain.Localization
{
    /// <summary>
    /// Represents a translation property
    /// </summary>
    public partial class TranslationEntity : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the locale key
        /// </summary>
        public string LocaleKey { get; set; }

        /// <summary>
        /// Gets or sets the locale value
        /// </summary>
        public string LocaleValue { get; set; }
        
    }
}
