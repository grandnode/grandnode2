namespace Grand.Domain.Orders;

public class MerchandiseReturnItem : BaseEntity
{
    /// <summary>
    ///     Gets or sets the order item identifier
    /// </summary>
    public string OrderItemId { get; set; }

    /// <summary>
    ///     Gets or sets the merchandise return reason identifier; null when it is not known
    /// </summary>
    public string ReasonId { get; set; }

    /// <summary>
    ///     Gets or sets the reason for return: its name in the customer's language when submitted
    /// </summary>
    public string ReasonForReturn { get; set; }

    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the merchandise return action identifier; null when it is not known
    /// </summary>
    public string ActionId { get; set; }

    /// <summary>
    ///     Gets or sets the requested action: its name in the customer's language when submitted
    /// </summary>
    public string RequestedAction { get; set; }
}