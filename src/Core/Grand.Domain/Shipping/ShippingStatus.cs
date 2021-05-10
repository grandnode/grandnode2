namespace Grand.Domain.Shipping
{
    /// <summary>
    /// Represents the shipping status enumeration
    /// </summary>
    public enum ShippingStatus
    {
        /// <summary>
        /// Shipping not required
        /// </summary>
        ShippingNotRequired = 0,
        /// <summary>
        /// Pending - not yet shipped
        /// </summary>
        Pending = 10,
        /// <summary>
        /// Prepared to shipped
        /// </summary>
        PreparedToShipped = 15,
        /// <summary>
        /// Partially shipped
        /// </summary>
        PartiallyShipped = 25,
        /// <summary>
        /// Shipped
        /// </summary>
        Shipped = 30,
        /// <summary>
        /// Delivered
        /// </summary>
        Delivered = 40,
    }
}
