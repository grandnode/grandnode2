namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The answer of one order placement rule
/// </summary>
/// <param name="Decision">What the rule decides</param>
/// <param name="Reason">Plain text shown to the buyer and to whoever decides the approval</param>
/// <param name="StoreDecides">The store decides the approval, also for a sub-account's order</param>
public record OrderPlacementRuleResult(OrderPlacementDecision Decision, string Reason, bool StoreDecides = false)
{
    public static OrderPlacementRuleResult Allow()
    {
        return new OrderPlacementRuleResult(OrderPlacementDecision.Allow, null);
    }

    public static OrderPlacementRuleResult RequireApproval(string reason)
    {
        return new OrderPlacementRuleResult(OrderPlacementDecision.RequireApproval, reason);
    }

    public static OrderPlacementRuleResult RequireStoreApproval(string reason)
    {
        return new OrderPlacementRuleResult(OrderPlacementDecision.RequireApproval, reason, true);
    }

    public static OrderPlacementRuleResult Block(string reason)
    {
        return new OrderPlacementRuleResult(OrderPlacementDecision.Block, reason);
    }
}
