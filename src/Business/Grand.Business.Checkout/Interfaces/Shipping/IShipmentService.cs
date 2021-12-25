using Grand.Domain;
using Grand.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Shipping
{
    /// <summary>
    /// Shipment service interface
    /// </summary>
    public partial interface IShipmentService
    {
        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="shippingCountryId">Shipping country identifier; "" to load all records</param>
        /// <param name="shippingStateId">Shipping state identifier; "" to load all records</param>
        /// <param name="shippingCity">Shipping city; null to load all records</param>
        /// <param name="trackingNumber">Search by tracking number</param>
        /// <param name="loadNotShipped">A value indicating whether we should load only not shipped shipments</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Shipments</returns>
        Task<IPagedList<Shipment>> GetAllShipments(string storeId = "", string vendorId = "", string warehouseId = "",
            string shippingCountryId = "",
            int shippingStateId = 0,
            string shippingCity = null,
            string trackingNumber = null,
            bool loadNotShipped = false,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Get shipment by identifiers
        /// </summary>
        /// <param name="shipmentIds">Shipment identifiers</param>
        /// <returns>Shipments</returns>
        Task<IList<Shipment>> GetShipmentsByIds(string[] shipmentIds);

        Task<IList<Shipment>> GetShipmentsByOrder(string orderId);

        /// <summary>
        /// Gets a shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>Shipment</returns>
        Task<Shipment> GetShipmentById(string shipmentId);

        /// <summary>
        /// Inserts a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        Task InsertShipment(Shipment shipment);

        /// <summary>
        /// Updates the shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        Task UpdateShipment(Shipment shipment);

        /// <summary>
        /// Deletes a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        Task DeleteShipment(Shipment shipment);

        #region Shipment notes

        /// <summary>
        /// Deletes an shipment note
        /// </summary>
        /// <param name="shipmentNote">The shipment note</param>
        Task DeleteShipmentNote(ShipmentNote shipmentNote);

        /// <summary>
        /// Insert an shipment note
        /// </summary>
        /// <param name="shipmentNote">The shipment note</param>
        Task InsertShipmentNote(ShipmentNote shipmentNote);


        /// <summary>
        /// Get shipmentnotes for shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>ShipmentNote</returns>
        Task<IList<ShipmentNote>> GetShipmentNotes(string shipmentId);

        /// <summary>
        /// Get shipmentnote by id
        /// </summary>
        /// <param name="shipmentnoteId">Shipment note identifier</param>
        /// <returns>ShipmentNote</returns>
        Task<ShipmentNote> GetShipmentNote(string shipmentnoteId);

        #endregion

    }
}
