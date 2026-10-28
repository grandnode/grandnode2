namespace Grand.Domain.Orders;

/// <summary>
///     Represents the order totals as the buyer saw them on the confirm step; all values are formatted
///     in the buyer's currency
/// </summary>
public class OrderApprovalSummary
{
    /// <summary>
    ///     Gets or sets the sub total
    /// </summary>
    public string SubTotal { get; set; }

    /// <summary>
    ///     Gets or sets the discount
    /// </summary>
    public string Discount { get; set; }

    /// <summary>
    ///     Gets or sets the shipping method name
    /// </summary>
    public string ShippingMethod { get; set; }

    /// <summary>
    ///     Gets or sets the shipping cost
    /// </summary>
    public string Shipping { get; set; }

    /// <summary>
    ///     Gets or sets the tax
    /// </summary>
    public string Tax { get; set; }

    /// <summary>
    ///     Gets or sets the order total
    /// </summary>
    public string Total { get; set; }
}
