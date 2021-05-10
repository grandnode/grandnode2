using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Directory
{
    /// <summary>
    /// Represents a state/province
    /// </summary>
    public partial class StateProvince : SubBaseEntity, ITranslationEntity
    {
        public StateProvince()
        {
            Locales = new List<TranslationEntity>();
        }
        
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the abbreviation
        /// </summary>
        public string Abbreviation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published
        /// </summary>
        public bool Published { get; set; }

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
