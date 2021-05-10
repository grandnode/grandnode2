using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a specification attribute option
    /// </summary>
    public partial class SpecificationAttributeOption : SubBaseEntity, ITranslationEntity
    {
        public SpecificationAttributeOption()
        {
            Locales = new List<TranslationEntity>();
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sename
        /// </summary>
        public string SeName { get; set; }

        // <summary>
        /// Gets or sets the color RGB value (used when you want to display "Color squares" instead of text)
        /// </summary>
        public string ColorSquaresRgb { get; set; }

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
