namespace Grand.Domain.Orders;

/// <summary>
///     Represents one paid cycle of a recurring payment
/// </summary>
public class RecurringPaymentCycle : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the cycle number; 1 is the initial order
    /// </summary>
    public int CycleNumber { get; set; }

    /// <summary>
    ///     Gets or sets the order the cycle was paid with
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    ///     Gets or sets the gateway's invoice or charge identifier
    /// </summary>
    public string ProviderReference { get; set; }

    /// <summary>
    ///     Gets or sets the amount the gateway charged
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the gateway charged the cycle
    /// </summary>
    public DateTime PaidOnUtc { get; set; }
}
