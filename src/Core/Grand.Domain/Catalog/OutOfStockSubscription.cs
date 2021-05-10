using Grand.Domain.Common;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a out of stock subscription
    /// </summary>
    public partial class OutOfStockSubscription : BaseEntity
    {
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the attribute 
        /// </summary>
        public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

        /// <summary>
        /// Gets or sets the attribute info (format)
        /// </summary>
        public string AttributeInfo { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public string WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

    }

}
