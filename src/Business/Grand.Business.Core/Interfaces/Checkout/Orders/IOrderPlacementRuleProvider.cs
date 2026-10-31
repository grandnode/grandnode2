using Grand.Business.Core.Utilities.Checkout;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     A rule a plugin adds to order placement: it allows the order, requires an approval for it, or blocks it.
///     Any number of rules can be active. If any rule blocks, the order cannot be placed; otherwise, if any rule
///     requires an approval, the order waits for one. A rule that allows the order never cancels another rule's decision.
///     <list type="bullet">
///         <item>
///             A rule runs several times per checkout, also on a page view: it must be fast and have no side effects.
///         </item>
///         <item>
///             The payment method and the total can be null early in checkout, and the rule must decide sensibly then.
///             The call made when the order is placed is the authoritative one.
///         </item>
///         <item>
///             The reason is shown to the buyer and to whoever decides the approval: plain text, nothing confidential.
///         </item>
///         <item>
///             A rule that throws blocks the order, so a fault cannot switch a purchasing control off.
///         </item>
///         <item>
///             By default a sub-account's company decides the approval. Return
///             <see cref="OrderPlacementRuleResult.RequireStoreApproval" /> for a control the buyer's company must not
///             release; the store then decides, whichever rule asked.
///         </item>
///         <item>
///             Do not inject <see cref="IOrderPlacementService" />. Use <see cref="OrderPlacementContext.GetTotal" /> for
///             the total, not <see cref="IOrderCalculationService" />.
///         </item>
///     </list>
/// </summary>
public interface IOrderPlacementRuleProvider : IProvider
{
    /// <summary>
    ///     Decides about the order being placed
    /// </summary>
    /// <param name="context">The order being placed</param>
    /// <param name="cancellationToken">Cancelled when the buyer's request is aborted; pass it to any external call</param>
    Task<OrderPlacementRuleResult> Evaluate(OrderPlacementContext context,
        CancellationToken cancellationToken = default);
}
