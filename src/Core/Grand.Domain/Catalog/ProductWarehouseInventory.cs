namespace Grand.Domain.Catalog
{
    /// <summary>
    /// Manage product inventory per warehouse
    /// </summary>
    public partial class ProductWarehouseInventory : SubBaseEntity
    {
        /// <summary>
        /// Gets or sets the warehouse identifier
        /// </summary>
        public string WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the stock quantity
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Gets or sets the reserved quantity (ordered but not shipped yet)
        /// </summary>
        public int ReservedQuantity { get; set; }

    }
}
