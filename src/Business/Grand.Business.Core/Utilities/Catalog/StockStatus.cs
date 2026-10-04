#nullable enable

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     The stock of a product as the customer is told about it.
/// </summary>
/// <param name="Availability">
///     null when it cannot be stated - the selected attributes match no combination
/// </param>
/// <param name="Resource">the message resource key; empty when the product does not display its stock</param>
/// <param name="Arg0">the message argument (the quantity), if any</param>
public record StockStatus(ProductAvailability? Availability, string Resource, object? Arg0);
