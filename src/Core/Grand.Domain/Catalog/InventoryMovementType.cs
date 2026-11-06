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

    Shipment = 10,
    ShipmentReversal = 11,
    Receipt = 20,
    TransferOut = 21,
    TransferIn = 22,
    Adjustment = 23,
    StockCount = 24,
    ReturnReceipt = 25,
    ManualEdit = 30,
    Api = 31,
    Import = 32,
    ProductCopy = 33
}
