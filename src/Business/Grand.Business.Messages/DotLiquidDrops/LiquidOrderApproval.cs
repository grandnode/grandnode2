using DotLiquid;

namespace Grand.Business.Messages.DotLiquidDrops;

/// <summary>
///     Tokens of an order approval request. The command handler fills every value, so the getters never touch a service.
/// </summary>
public class LiquidOrderApproval : Drop
{
    public LiquidOrderApproval()
    {
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string BuyerName { get; set; }

    public string BuyerEmail { get; set; }

    public string Status { get; set; }

    /// <summary>
    ///     The order total, already formatted in the buyer's currency
    /// </summary>
    public string Total { get; set; }

    /// <summary>
    ///     The cart lines as an HTML table; every value is HTML-encoded
    /// </summary>
    public string ItemsHtml { get; set; }

    /// <summary>
    ///     The approver's comment; HTML-encoded
    /// </summary>
    public string DecisionComment { get; set; }

    /// <summary>
    ///     Absolute link to the approval request
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    ///     Absolute link to the buyer's cart
    /// </summary>
    public string CartUrl { get; set; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}
