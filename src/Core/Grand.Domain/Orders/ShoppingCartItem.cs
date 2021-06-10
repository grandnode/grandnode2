using Grand.Domain.Common;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents a shopping cart item
    /// </summary>
    public partial class ShoppingCartItem : SubBaseEntity
    {

        public ShoppingCartItem()
        {
            Attributes = new List<CustomAttribute>();
        }
        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public string WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart type identifier
        /// </summary>
        public ShoppingCartType ShoppingCartTypeId { get; set; }

        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the custom attributes (see "ProductAttribute" entity for more info)
        /// </summary>
        public IList<CustomAttribute> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the price enter by a customer
        /// </summary>
        public double? EnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the rental product start date (null if it's not a rental product)
        /// </summary>
        public DateTime? RentalStartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the rental product end date (null if it's not a rental product)
        /// </summary>
        public DateTime? RentalEndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether the shopping cart item is free shipping
        /// </summary>
        public bool IsFreeShipping { get; set; }

        /// <summary>
        /// Gets a value indicating whether the shopping cart item is gift voucher
        /// </summary>
        public bool IsGiftVoucher { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether the shopping cart item is ship enabled
        /// </summary>
        public bool IsShipEnabled { get; set; }

        /// <summary>
        /// Gets a value the additional shipping charge for product
        /// </summary>
        public double AdditionalShippingChargeProduct { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether the shopping cart item is tax exempt
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets the reservation identifier
        /// </summary>
        public string ReservationId { get; set; }

        /// <summary>
        /// Gets or sets parameter of reservation
        /// </summary>
        public string Parameter { get; set; }

        /// <summary>
        /// Gets or sets Duration of reservation
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets owner cid
        /// </summary>
        public string cId { get; set; }
    }
}
