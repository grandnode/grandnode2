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
    ///     In transit with the carrier
    /// </summary>
    InTransit = 10,

    /// <summary>
    ///     Delivered to the store
    /// </summary>
    Delivered = 20
}
