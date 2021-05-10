namespace Grand.Domain.Customers
{
    /// <summary>
    /// Represents a customer group products
    /// </summary>
    public partial class CustomerGroupProduct : BaseEntity
    {

        /// <summary>
        /// Gets or sets the customer group id
        /// </summary>
        public string CustomerGroupId { get; set; }

        /// <summary>
        /// Gets or sets the product Id
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

    }
    

}