namespace Grand.Domain.Catalog;

/// <summary>
///     What caused an inventory journal entry
/// </summary>
public enum InventoryMovementType
{
    /// <summary>
    ///     The stock a bucket held when its history started; sums count from the latest one
    /// </summary>
    OpeningBalance = 0,

    /// <summary>
    ///     An entry written before the journal recorded every change; shown, never counted
    /// </summary>
    Legacy = 1,

    /// <summary>
    ///     The stock a bucket held when another inventory method or warehouse mode took it out of use
    /// </summary>
    ClosingBalance = 2,

    /// <summary>
    ///     Batch or serial tracking was switched on; the stock the bucket held becomes stock without units. Moves
    ///     nothing
    /// </summary>
    TrackingStarted = 3,

    Shipment = 10,
    ShipmentReversal = 11,
    Receipt = 20,
    TransferOut = 21,
    TransferIn = 22,
    Adjustment = 23,
    StockCount = 24,
    ReturnReceipt = 25,

    /// <summary>
    ///     Stock without units gets batches or serial numbers; in and out are equal, the stock does not change
    /// </summary>
    TrackingAssignment = 26,

    /// <summary>
    ///     A returned unit with a serial number is scrapped; it was not in stock, so the stock does not change
    /// </summary>
    ReturnScrapped = 27,
    ManualEdit = 30,
    Api = 31,
    Import = 32,
    ProductCopy = 33
}
