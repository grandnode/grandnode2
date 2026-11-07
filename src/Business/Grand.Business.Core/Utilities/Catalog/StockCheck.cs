#nullable enable

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Whether the stock covers a quantity, by the rule the shopping cart and the order placement share.
/// </summary>
/// <param name="Covered">the quantity can be sold: the stock covers it, or the stock does not limit it</param>
/// <param name="StockQuantity">the quantity on stock that was counted; 0 when nothing was counted</param>
/// <param name="CombinationMissing">the selected attributes match no combination of a product stocked by them</param>
public record StockCheck(bool Covered, int StockQuantity, bool CombinationMissing);
