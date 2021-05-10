using Grand.Domain.Localization;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Messages
{
    /// <summary>
    /// Represents a banner
    /// </summary>
    public partial class Banner : BaseEntity, ITranslationEntity
    {
        public Banner()
        {
            Locales = new List<TranslationEntity>();
        }
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        
    }
}
