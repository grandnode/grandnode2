using Grand.Domain.Common;

namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Represents a product attribute combination
    /// </summary>
    public partial class ProductAttributeCombination : SubBaseEntity, ICloneable
    {
        private ICollection<ProductCombinationWarehouseInventory> _warehouseInventory;
        private ICollection<ProductCombinationTierPrices> _tierPrices;

        /// <summary>
        /// Gets or sets the custom attributes (see "ProductAttribute" entity for more info)
        /// </summary>
        public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

        /// <summary>
        /// Gets or sets the stock quantity
        /// </summary>
        public int StockQuantity { get; set; }
        /// <summary>
        /// Gets or sets the reserved quantity (ordered but not shipped yet)
        /// </summary>
        public int ReservedQuantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow orders when out of stock
        /// </summary>
        public bool AllowOutOfStockOrders { get; set; }

        /// <summary>
        /// Gets or sets the text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the SKU
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the collection part number
        /// </summary>
        public string Mpn { get; set; }

        /// <summary>
        /// Gets or sets the Global Trade Item Number (GTIN). These identifiers include UPC (in North America), EAN (in Europe), JAN (in Japan), and ISBN (for books).
        /// </summary>
        public string Gtin { get; set; }

        /// <summary>
        /// Gets or sets the attribute combination price. This way a store owner can override the default product price when this attribute combination is added to the cart. For example, you can give a discount this way.
        /// </summary>
        public double? OverriddenPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity when admin should be notified
        /// </summary>
        public int NotifyAdminForQuantityBelow { get; set; }

        /// <summary>
        /// Gets or sets the identifier of picture associated with this combination
        /// </summary>
        public string PictureId { get; set; }

        /// <summary>
        /// Gets or sets the collection of "ProductCombinationWarehouseInventory". We use it only when "UseMultipleWarehouses" is set to "true"
        /// </summary>
        public virtual ICollection<ProductCombinationWarehouseInventory> WarehouseInventory
        {
            get { return _warehouseInventory ??= new List<ProductCombinationWarehouseInventory>(); }
            protected set { _warehouseInventory = value; }
        }

        /// <summary>
        /// Gets or sets the collection of "ProductCombinationTierPrices". 
        /// </summary>
        public virtual ICollection<ProductCombinationTierPrices> TierPrices
        {
            get { return _tierPrices ??= new List<ProductCombinationTierPrices>(); }
            protected set { _tierPrices = value; }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
