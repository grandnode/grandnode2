using Grand.Domain.Localization;

namespace Grand.Domain.Messages
{
    /// <summary>
    /// Represents a contact attribute value
    /// </summary>
    public partial class ContactAttributeValue : SubBaseEntity, ITranslationEntity
    {
        public ContactAttributeValue()
        {
            Locales = new List<TranslationEntity>();
        }

        /// <summary>
        /// Gets or sets the checkout attribute mapping identifier
        /// </summary>
        public string ContactAttributeId { get; set; }

        /// <summary>
        /// Gets or sets the checkout attribute name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the color RGB value (used with "Color squares" attribute type)
        /// </summary>
        public string ColorSquaresRgb { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is pre-selected
        /// </summary>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
    }

}
