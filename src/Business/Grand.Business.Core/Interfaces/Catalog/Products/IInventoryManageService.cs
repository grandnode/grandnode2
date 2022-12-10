﻿using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    public interface IInventoryManageService
    {
        #region Inventory management methods

        /// <summary>
        /// Updates stock the product
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="mediator">Notification</param>
        Task UpdateStockProduct(Product product, bool mediator = true);

        /// <summary>
        /// Adjust reserved inventory
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="quantityToChange">Quantity to increase or decrease</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="warehouseId">Warehouse ident</param>
        Task AdjustReserved(Product product, int quantityToChange, IList<CustomAttribute> attributes = null, string warehouseId = "");


        /// <summary>
        /// Book the reserved quantity
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="shipment">Shipment</param>
        /// <param name="shipmentItem">Shipment item</param>
        Task BookReservedInventory(Product product, Shipment shipment, ShipmentItem shipmentItem);

        /// <summary>
        /// Reverse booked inventory
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="shipmentItem">Shipment item</param>
        /// <returns>Quantity reversed</returns>
        Task ReverseBookedInventory(Shipment shipment, ShipmentItem shipmentItem);

        #endregion

    }
}
