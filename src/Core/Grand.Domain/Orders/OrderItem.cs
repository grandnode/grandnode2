using Grand.Domain.Common;
using System;
using System.Collections.Generic;

namespace Grand.Domain.Orders
{
    /// <summary>
    /// Represents an order item
    /// </summary>
    public partial class OrderItem : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the order item identifier
        /// </summary>
        public Guid OrderItemGuid { get; set; }

        /// <summary>
        /// Gets or sets the product identifier
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the sku product identifier
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the vendor identifier
        /// </summary>
        public string VendorId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public string WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the sales employee identifier 
        /// </summary>
        public string SeId { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets item is ship
        /// </summary>
        public bool IsShipEnabled { get; set; }

        /// <summary>
        /// Gets or sets the open quantity
        /// </summary>
        public int OpenQty { get; set; }

        /// <summary>
        /// Gets or sets the open quantity
        /// </summary>
        public int ShipQty { get; set; }

        /// <summary>
        /// Gets or sets the cancel quantity
        /// </summary>
        public int CancelQty { get; set; }

        /// <summary>
        /// Gets or sets the cancel amount
        /// </summary>
        public double CancelAmount { get; set; }

        /// <summary>
        /// Gets or sets the return quantity
        /// </summary>
        public int ReturnQty { get; set; }

        /// <summary>
        /// Gets or sets the open quantity
        /// </summary>
        public OrderItemStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the tax rate
        /// </summary>
        public double TaxRate { get; set; }

        /// <summary>
        /// Gets or sets the unit price without discount in primary store currency (incl tax)
        /// </summary>
        public double UnitPriceWithoutDiscInclTax { get; set; }

        /// <summary>
        /// Gets or sets the unit price without discount in primary store currency (excl tax)
        /// </summary>
        public double UnitPriceWithoutDiscExclTax { get; set; }

        /// <summary>
        /// Gets or sets the unit price in primary store currency (incl tax)
        /// </summary>
        public double UnitPriceInclTax { get; set; }

        /// <summary>
        /// Gets or sets the unit price in primary store currency (excl tax)
        /// </summary>
        public double UnitPriceExclTax { get; set; }

        /// <summary>
        /// Gets or sets the price in primary store currency (incl tax)
        /// </summary>
        public double PriceInclTax { get; set; }

        /// <summary>
        /// Gets or sets the price in primary store currency (excl tax)
        /// </summary>
        public double PriceExclTax { get; set; }

        /// <summary>
        /// Gets or sets the discount amount (incl tax)
        /// </summary>
        public double DiscountAmountInclTax { get; set; }

        /// <summary>
        /// Gets or sets the discount amount (excl tax)
        /// </summary>
        public double DiscountAmountExclTax { get; set; }

        /// <summary>
        /// Gets or sets the original cost of this order item (when an order was placed), qty 1
        /// </summary>
        public double OriginalProductCost { get; set; }

        /// <summary>
        /// Gets or sets the attribute description
        /// </summary>
        public string AttributeDescription { get; set; }

        public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

        /// <summary>
        /// Gets or sets the download count
        /// </summary>
        public int DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether download is activated
        /// </summary>
        public bool IsDownloadActivated { get; set; }

        /// <summary>
        /// Gets or sets a license download identifier (in case this is a downloadable product)
        /// </summary>
        public string LicenseDownloadId { get; set; }

        /// <summary>
        /// Gets or sets the total weight of one item
        /// </summary>       
        public double? ItemWeight { get; set; }

        /// <summary>
        /// Gets or sets the rental product start date (null if it's not a rental product)
        /// </summary>
        public DateTime? RentalStartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the rental product end date (null if it's not a rental product)
        /// </summary>
        public DateTime? RentalEndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of order creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the vendor`s commission
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// Gets or sets owner cid
        /// </summary>
        public string cId { get; set; }
    }
}