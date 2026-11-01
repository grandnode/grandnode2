namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The combined answer of all order placement rules
/// </summary>
/// <param name="Decision">Any block blocks; otherwise any required approval requires it; otherwise the order is allowed</param>
/// <param name="Reasons">The distinct reasons of the rules that produced this decision</param>
/// <param name="StoreDecides">A rule requiring the approval wants the store to decide it</param>
public record OrderPlacementEvaluation(OrderPlacementDecision Decision, IReadOnlyList<string> Reasons,
    bool StoreDecides = false);
