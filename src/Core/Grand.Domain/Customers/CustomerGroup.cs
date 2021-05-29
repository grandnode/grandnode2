namespace Grand.Domain.Customers
{
    /// <summary>
    /// Represents a customer group
    /// </summary>
    public partial class CustomerGroup : BaseEntity
    {

        /// <summary>
        /// Gets or sets the customer group name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer group is marked as free shiping
        /// </summary>
        public bool FreeShipping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer group is marked as tax exempt
        /// </summary>
        public bool TaxExempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer group is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customer group is system
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// Gets or sets the customer group display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the customer group system name
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the customers must change passwords after a specified time
        /// </summary>
        public bool EnablePasswordLifetime { get; set; }

        /// <summary>
        /// Gets or sets a minimum order total amount
        /// </summary>
        public double? MinOrderAmount { get; set; }

        /// <summary>
        /// Gets or sets a maximum order total amount
        /// </summary>
        public double? MaxOrderAmount { get; set; }
    }

}