namespace Grand.Business.Core.Utilities.System;

/// <summary>
///     What the merchandise return report counts returns by
/// </summary>
public enum MerchandiseReturnReportGrouping
{
    Reason = 0,
    Product = 10,
    Vendor = 20
}

/// <summary>
///     Represents a merchandise return report line: the returns of one reason, product or vendor in the period
/// </summary>
public class MerchandiseReturnReportLine
{
    /// <summary>
    ///     Gets or sets what the line counts: a reason identifier (or the reason's text for a return item without
    ///     one), a product identifier, or a vendor identifier ("" for the store's own items)
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///     Gets or sets the reason's text as the customer chose it, for a reason no longer found; null otherwise
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the number of returns
    /// </summary>
    public int ReturnCount { get; set; }

    /// <summary>
    ///     Gets or sets the units returned
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the units of the product sold in the period; null for a reason or a vendor
    /// </summary>
    public int? SoldQuantity { get; set; }
}
