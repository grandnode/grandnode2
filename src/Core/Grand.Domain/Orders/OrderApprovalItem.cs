namespace Grand.Domain.Orders;

/// <summary>
///     Represents a cart line as the approver sees it
/// </summary>
public class OrderApprovalItem : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the product identifier
    /// </summary>
    public string ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the product name
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    ///     Gets or sets the product SKU
    /// </summary>
    public string Sku { get; set; }

    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the formatted attributes
    /// </summary>
    public string AttributeDescription { get; set; }

    /// <summary>
    ///     Gets or sets the formatted unit price, in the buyer's currency
    /// </summary>
    public string UnitPrice { get; set; }

    /// <summary>
    ///     Gets or sets the formatted line total, in the buyer's currency
    /// </summary>
    public string LineTotal { get; set; }
}
