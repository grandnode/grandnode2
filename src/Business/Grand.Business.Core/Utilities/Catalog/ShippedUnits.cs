using Grand.Domain.Shipping;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     The units one shipment shipped of an order item, and their warranty
/// </summary>
public sealed record ShippedUnit(
    string ShipmentId,
    int ShipmentNumber,
    DateTime ShippedOnUtc,
    IList<string> SerialNumbers,
    IList<ShipmentItemBatch> Batches,
    DateTime? WarrantyUntilUtc)
{
    public bool InWarranty(DateTime nowUtc)
    {
        return WarrantyUntilUtc > nowUtc;
    }
}

/// <summary>
///     What an order's shipments shipped of each order item, as the order views show it: read from the shipments, so
///     showing it reads no batch or serial number
/// </summary>
public static class ShippedUnits
{
    /// <summary>
    ///     The shipments of the order item that named units or a warranty, oldest first
    /// </summary>
    public static IList<ShippedUnit> Of(IEnumerable<Shipment> shipments, string orderItemId)
    {
        ArgumentNullException.ThrowIfNull(shipments);

        return shipments
            .SelectMany(shipment => shipment.ShipmentItems
                .Where(x => x.OrderItemId == orderItemId && ((x.SerialNumbers?.Count ?? 0) > 0 ||
                                                             (x.Batches?.Count ?? 0) > 0 ||
                                                             x.WarrantyUntilUtc.HasValue))
                .Select(x => new ShippedUnit(shipment.Id, shipment.ShipmentNumber, shipment.CreatedOnUtc,
                    x.SerialNumbers ?? [], x.Batches ?? [], x.WarrantyUntilUtc)))
            .OrderBy(x => x.ShippedOnUtc)
            .ToList();
    }
}
