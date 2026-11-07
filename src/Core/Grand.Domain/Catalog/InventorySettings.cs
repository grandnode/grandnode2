using Grand.Domain.Configuration;

namespace Grand.Domain.Catalog;

/// <summary>
///     Inventory settings. A system-wide setting, stored with an empty store id and never overridden per store: a
///     store-scoped row would freeze every field of the class for that store
/// </summary>
public class InventorySettings : ISettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether stock changes only through inventory documents; the stock fields of
    ///     the back-office forms are read-only then
    /// </summary>
    public bool StockChangesOnlyThroughDocuments { get; set; }

    /// <summary>
    ///     Gets or sets the number prefix of a receipt
    /// </summary>
    public string ReceiptNumberPrefix { get; set; } = "PZ";

    /// <summary>
    ///     Gets or sets the number prefix of a transfer
    /// </summary>
    public string TransferNumberPrefix { get; set; } = "MM";

    /// <summary>
    ///     Gets or sets the number prefix of an adjustment
    /// </summary>
    public string AdjustmentNumberPrefix { get; set; } = "KOR";

    /// <summary>
    ///     Gets or sets the number prefix of a stock count
    /// </summary>
    public string StockCountNumberPrefix { get; set; } = "INW";

    /// <summary>
    ///     Gets or sets the number prefix of a return receipt
    /// </summary>
    public string ReturnReceiptNumberPrefix { get; set; } = "ZW";

    /// <summary>
    ///     Gets or sets the number prefix of an assignment of batches or serial numbers to stock without them
    /// </summary>
    public string AssignmentNumberPrefix { get; set; } = "PRZ";

    /// <summary>
    ///     Gets or sets the days before the expiry date a batch is reported as expiring
    /// </summary>
    public int ExpiryWarningDays { get; set; } = 30;
}
