using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a merchandise return reason
    /// </summary>
    public partial class MerchandiseReturnReason : BaseEntity, ITranslationEntity
    {
        public MerchandiseReturnReason()
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
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
    }
}
