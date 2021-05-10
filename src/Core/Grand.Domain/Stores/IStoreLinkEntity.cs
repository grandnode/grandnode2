using System.Collections.Generic;

namespace Grand.Domain.Stores
{
    /// <summary>
    /// Represents an entity which user store linking
    /// </summary>
    public partial interface IStoreLinkEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited to some stores
        /// </summary>
        bool LimitedToStores { get; set; }
        IList<string> Stores { get; set; }
    }
}
