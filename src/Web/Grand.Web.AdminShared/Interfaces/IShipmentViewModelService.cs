using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Interfaces;

public interface IShipmentViewModelService
{
    Task<ShipmentModel> PrepareShipmentModel(Shipment shipment, bool prepareProducts,
        bool prepareShipmentEvent = false);

    Task<int> GetStockQty(Product product, string warehouseId);
    Task<int> GetReservedQty(Product product, string warehouseId);
    Task<IList<ShipmentModel.ShipmentNote>> PrepareShipmentNotes(Shipment shipment);
    Task InsertShipmentNote(Shipment shipment, string downloadId, bool displayToCustomer, string message);
    Task DeleteShipmentNote(Shipment shipment, string id);
    Task<ShipmentListModel> PrepareShipmentListModel();
    Task<ShipmentModel> PrepareShipmentModel(Order order);

    Task<(Shipment shipment, double? totalWeight)> PrepareShipment(Order order, IEnumerable<OrderItem> orderItems,
        AddShipmentModel model);

    Task<(bool valid, string message)> ValidStockShipment(Shipment shipment);

    /// <summary>
    ///     Checks the batches and serial numbers a shipment names and fixes the warranty of its units
    /// </summary>
    /// <returns>The messages, one an item that cannot ship, naming its product; empty when it can</returns>
    Task<IList<string>> PrepareShipmentUnits(Shipment shipment);

    Task<(IEnumerable<Shipment> shipments, int totalCount)> PrepareShipments(ShipmentListModel model, int pageIndex,
        int pageSize);
}