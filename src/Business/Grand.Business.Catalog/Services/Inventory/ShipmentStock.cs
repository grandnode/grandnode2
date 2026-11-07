using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;

namespace Grand.Business.Catalog.Services.Inventory;

/// <summary>
///     The stock changes a shipment item books: the booking applies them, and the check before the shipment is stored
///     asks the ledger about the same changes
/// </summary>
internal static class ShipmentStock
{
    public static StockSource Source(Shipment shipment, ShipmentItem shipmentItem)
    {
        return new StockSource(nameof(Shipment), shipment.Id, shipmentItem.Id, shipment.ShipmentNumber.ToString(),
            Comment: $"Shipment - {shipment.TrackingNumber}");
    }

    /// <summary>
    ///     Its quantity, with the batches and serial numbers it names for a product tracking them; the quantity no
    ///     batch names comes from the stock without one
    /// </summary>
    public static List<StockChange> Changes(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        StockChange Change(int quantity) => new(shipmentItem.Attributes ?? [], shipmentItem.WarehouseId, -quantity,
            false, InventoryMovementType.Shipment, Source(shipment, shipmentItem));

        if (!InventoryTracking.Tracked(product))
            return [Change(shipmentItem.Quantity)];
        if (product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber)
            return [Change(shipmentItem.Quantity) with { SerialNumbers = (shipmentItem.SerialNumbers ?? []).ToList() }];

        var changes = (shipmentItem.Batches ?? []).Where(x => x.Quantity > 0)
            .GroupBy(x => UnitNumbers.Key(x.Number))
            .Select(x => Change(x.Sum(y => y.Quantity)) with { BatchNumber = x.First().Number })
            .ToList();
        var withoutBatch = shipmentItem.Quantity + changes.Sum(x => x.Quantity);
        if (withoutBatch > 0)
            changes.Add(Change(withoutBatch));
        return changes;
    }
}
