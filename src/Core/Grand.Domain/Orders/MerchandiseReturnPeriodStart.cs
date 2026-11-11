namespace Grand.Domain.Orders;

/// <summary>
///     Represents the moment the days for a merchandise return are counted from
/// </summary>
public enum MerchandiseReturnPeriodStart
{
    /// <summary>
    ///     The order was placed
    /// </summary>
    OrderPlaced = 0,

    /// <summary>
    ///     The shipment was delivered: each shipment of the order has its own days
    /// </summary>
    Delivered = 10
}
