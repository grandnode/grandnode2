using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     The built-in rule: an order of a company sub-account over its limit waits for an approval
/// </summary>
public class OrderApprovalLimitRuleProvider : IOrderPlacementRuleProvider
{
    private readonly ICurrencyService _currencyService;
    private readonly CustomerSettings _customerSettings;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITranslationService _translationService;

    public OrderApprovalLimitRuleProvider(CustomerSettings customerSettings, ICurrencyService currencyService,
        IPriceFormatter priceFormatter, ITranslationService translationService)
    {
        _customerSettings = customerSettings;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _translationService = translationService;
    }

    public string ConfigurationUrl => null;
    public string SystemName => "OrderPlacementRule.ApprovalLimit";
    public string FriendlyName => SystemName;
    public int Priority => 0;
    public IList<string> LimitedToStores => [];
    public IList<string> LimitedToGroups => [];

    public async Task<OrderPlacementRuleResult> Evaluate(OrderPlacementContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var customer = context.Calculation.Customer;
        var limit = customer.OrderApprovalLimit;

        // not a limited buyer: returning here keeps a retail checkout free of a total calculation
        if (_customerSettings.HideSubAccountsTab || string.IsNullOrEmpty(customer.OwnerId) ||
            OrderApprovalRules.CanApprove(customer) || !limit.HasValue)
            return OrderPlacementRuleResult.Allow();

        // the total is only known once shipping is chosen; until then the rule is not evaluated
        var total = await context.GetTotal();
        if (!total.HasValue || total.Value <= limit.Value)
            return OrderPlacementRuleResult.Allow();

        return OrderPlacementRuleResult.RequireApproval(await LimitExceededText(limit.Value, context));
    }

    private async Task<string> LimitExceededText(double limit, OrderPlacementContext context)
    {
        var currency = context.Calculation.Currency;
        var message = _translationService.GetResource("Checkout.OrderApproval.LimitExceeded", context.Language.Id);
        try
        {
            return string.Format(message,
                _priceFormatter.FormatPrice(await _currencyService.ConvertFromPrimaryStoreCurrency(limit, currency),
                    currency));
        }
        catch (FormatException)
        {
            //the text is editable by an operator: a broken placeholder must not break the checkout
            return message;
        }
    }
}
