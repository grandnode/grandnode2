using Grand.Domain.Localization;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a merchandise return action
    /// </summary>
    public partial class MerchandiseReturnAction : BaseEntity, ITranslationEntity
    {
        public MerchandiseReturnAction()
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
