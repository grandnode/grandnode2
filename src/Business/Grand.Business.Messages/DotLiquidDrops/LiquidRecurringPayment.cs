using DotLiquid;

namespace Grand.Business.Messages.DotLiquidDrops;

/// <summary>
///     Tokens of a recurring payment. The command handler fills every value, so the getters never touch a service.
///     The initial order's tokens come from the order drop
/// </summary>
public class LiquidRecurringPayment : Drop
{
    public LiquidRecurringPayment()
    {
        AdditionalTokens = new Dictionary<string, string>();
    }

    public int CycleLength { get; set; }

    /// <summary>
    ///     The cycle period, translated
    /// </summary>
    public string? CyclePeriod { get; set; }

    public int CyclesPaid { get; set; }

    /// <summary>
    ///     The total cycles; 0 means until cancelled
    /// </summary>
    public int TotalCycles { get; set; }

    /// <summary>
    ///     The reason the last cycle failed, as the gateway reported it; HTML-encoded
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    ///     Who cancelled: the customer, a staff name or the provider; HTML-encoded
    /// </summary>
    public string? CancelledBy { get; set; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}
