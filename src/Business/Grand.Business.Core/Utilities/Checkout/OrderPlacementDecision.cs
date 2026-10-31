namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     What an order placement rule decides about the order being placed
/// </summary>
public enum OrderPlacementDecision
{
    Allow = 0,
    RequireApproval = 10,
    Block = 20
}
