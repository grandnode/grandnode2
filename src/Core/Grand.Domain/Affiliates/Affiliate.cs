using Grand.Domain.Common;

namespace Grand.Domain.Affiliates
{
    /// <summary>
    /// Represents an affiliate
    /// </summary>
    public partial class Affiliate : BaseEntity
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the friendly url name for affiliate
        /// </summary>
        public string FriendlyUrlName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        public virtual Address Address { get; set; }
    }
}
