using Grand.Domain.Common;

namespace Grand.Domain.Shipping
{
    /// <summary>
    /// Represents a shipment
    /// </summary>
    public class PickupPoint : BaseEntity
    {
        /// <summary>
        /// Gets or sets the point name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the address identifier
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public string WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets a store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the pickup fee
        /// </summary>
        public double PickupFee { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the GeoCoordinate.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the GeoCoordinate.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}