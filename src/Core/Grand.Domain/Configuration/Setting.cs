
namespace Grand.Domain.Configuration
{
    /// <summary>
    /// Represents a setting
    /// </summary>
    public partial class Setting : BaseEntity
    {
        public Setting()
        {

        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the store for which this setting is valid. 0 is set when the setting is for all stores
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the metadata settings
        /// </summary>
        public string Metadata { get; set; }

    }
}
