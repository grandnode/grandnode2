using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Shipping
{
    /// <summary>
    /// Represents a delivery date 
    /// </summary>
    public partial class DeliveryDate : BaseEntity, ITranslationEntity
    {
        public DeliveryDate()
        {
            Locales = new List<TranslationEntity>();
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the color RGB value (used with "Color squares" attribute type)
        /// </summary>
        public string ColorSquaresRgb { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
    }
}