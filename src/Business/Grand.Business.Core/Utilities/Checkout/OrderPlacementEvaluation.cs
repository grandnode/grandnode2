namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The combined answer of all order placement rules
/// </summary>
/// <param name="Decision">Any block blocks; otherwise any required approval requires it; otherwise the order is allowed</param>
/// <param name="Reasons">The reasons of the rules that produced this decision</param>
public record OrderPlacementEvaluation(OrderPlacementDecision Decision, IReadOnlyList<string> Reasons);
