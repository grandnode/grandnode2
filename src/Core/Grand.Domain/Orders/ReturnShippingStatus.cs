namespace Grand.Domain.Orders;

/// <summary>
///     Represents the progress of the parcel of a merchandise return
/// </summary>
public enum ReturnShippingStatus
{
    /// <summary>
    ///     Not shipped yet
    /// </summary>
    NotShipped = 0,

    /// <summary>
    ///     Handed to the carrier
    /// </summary>
    Shipped = 10,

    /// <summary>
    ///     Delivered to the store
    /// </summary>
    Delivered = 20
}
