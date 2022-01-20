﻿using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Admin.Interfaces
{
    public interface IShipmentViewModelService
    {
        Task<ShipmentModel> PrepareShipmentModel(Shipment shipment, bool prepareProducts, bool prepareShipmentEvent = false);
        Task<int> GetStockQty(Product product, string warehouseId);
        Task<int> GetReservedQty(Product product, string warehouseId);
        Task LogShipment(string shipmentId, string message);
        Task<IList<ShipmentModel.ShipmentNote>> PrepareShipmentNotes(Shipment shipment);
        Task InsertShipmentNote(Shipment shipment, string downloadId, bool displayToCustomer, string message);
        Task DeleteShipmentNote(Shipment shipment, string id);
        Task<ShipmentListModel> PrepareShipmentListModel();
        Task<ShipmentModel> PrepareShipmentModel(Order order);
        Task<(Shipment shipment, double? totalWeight)> PrepareShipment(Order order, IList<OrderItem> orderItems, IFormCollection form);
        Task<(bool valid, string message)> ValidStockShipment(Shipment shipment);
        Task<(IEnumerable<Shipment> shipments, int totalCount)> PrepareShipments(ShipmentListModel model, int pageIndex, int pageSize);
    }
}
