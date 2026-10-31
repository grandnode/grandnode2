using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Order placement service
/// </summary>
public class OrderPlacementService : IOrderPlacementService
{
    private const int MaxReasonLength = 500;

    private readonly ILogger<OrderPlacementService> _logger;
    private readonly IOrderCalculationService _orderCalculationService;
    private readonly IEnumerable<IOrderPlacementRuleProvider> _rules;
    private readonly ITranslationService _translationService;

    public OrderPlacementService(IEnumerable<IOrderPlacementRuleProvider> rules,
        IOrderCalculationService orderCalculationService,
        ITranslationService translationService,
        ILogger<OrderPlacementService> logger)
    {
        _rules = rules;
        _orderCalculationService = orderCalculationService;
        _translationService = translationService;
        _logger = logger;
    }

    public virtual async Task<OrderPlacementEvaluation> Evaluate(IList<ShoppingCartItem> cart,
        CalculationContext calculation, Language language, string paymentMethodSystemName)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(calculation);
        ArgumentNullException.ThrowIfNull(language);

        var context = new OrderPlacementContext(cart, calculation, language, paymentMethodSystemName,
            async () => (await _orderCalculationService.GetShoppingCartTotal(cart, calculation))?.Total);

        // every rule runs, also after a block: the buyer sees all the reasons at once
        var results = new List<OrderPlacementRuleResult>();
        foreach (var rule in _rules
                     .Where(x => x.IsAuthenticateStore(calculation.Store) && x.IsAuthenticateGroup(calculation.Customer))
                     .OrderBy(x => x.Priority))
            results.Add(await Run(rule, context));

        var decision = results.Count > 0 ? results.Max(x => x.Decision) : OrderPlacementDecision.Allow;
        if (decision == OrderPlacementDecision.Allow)
            return new OrderPlacementEvaluation(decision, []);

        return new OrderPlacementEvaluation(decision, results
            .Where(x => x.Decision == decision)
            .Select(x => Reason(x, language))
            .ToList());
    }

    /// <summary>
    ///     A rule that fails blocks the order: a fault must not switch a purchasing control off, and the buyer never
    ///     reads the exception
    /// </summary>
    private async Task<OrderPlacementRuleResult> Run(IOrderPlacementRuleProvider rule, OrderPlacementContext context)
    {
        try
        {
            return await rule.Evaluate(context)
                   ?? throw new InvalidOperationException("The rule returned no result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order placement rule {SystemName} failed, the order is blocked", rule.SystemName);
            return OrderPlacementRuleResult.Block(
                _translationService.GetResource("Checkout.OrderPlacement.RuleFailed", context.Language.Id));
        }
    }

    private string Reason(OrderPlacementRuleResult result, Language language)
    {
        var reason = result.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            return _translationService.GetResource(result.Decision == OrderPlacementDecision.Block
                ? "Checkout.OrderPlacement.Blocked"
                : "Checkout.OrderPlacement.ApprovalRequired", language.Id);

        return reason.Length > MaxReasonLength ? reason[..MaxReasonLength] : reason;
    }
}
