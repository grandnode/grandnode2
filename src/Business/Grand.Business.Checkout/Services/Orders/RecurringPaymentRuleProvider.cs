using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     The built-in rule: the payment method runs the schedule of an order billed in cycles, so the order needs a method
///     that can, and something to pay
/// </summary>
public class RecurringPaymentRuleProvider : IOrderPlacementRuleProvider
{
    private readonly IPaymentService _paymentService;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly ITranslationService _translationService;

    public RecurringPaymentRuleProvider(IRecurringPaymentService recurringPaymentService,
        IPaymentService paymentService, ITranslationService translationService)
    {
        _recurringPaymentService = recurringPaymentService;
        _paymentService = paymentService;
        _translationService = translationService;
    }

    public string? ConfigurationUrl => null;
    public string SystemName => "OrderPlacementRule.RecurringPayment";
    public string FriendlyName => SystemName;
    public int Priority => 0;
    public IList<string> LimitedToStores => [];
    public IList<string> LimitedToGroups => [];

    public async Task<OrderPlacementRuleResult> Evaluate(OrderPlacementContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // not a recurring cart: returning here keeps every other checkout free of a total calculation
        if (!await _recurringPaymentService.HasRecurringProducts(context.Cart))
            return OrderPlacementRuleResult.Allow();

        // a zero total skips the payment step and clears the method, so nothing would run the schedule
        if (await context.GetTotal() is 0)
            return OrderPlacementRuleResult.Block(
                _translationService.GetResource("Checkout.RecurringPayment.NothingToPay", context.Language.Id));

        // a method that charges once would break the promise of a product billed in cycles; none is chosen yet early
        // in checkout
        if (!string.IsNullOrEmpty(context.PaymentMethodSystemName) &&
            !await _paymentService.SupportRecurringPayment(context.PaymentMethodSystemName))
            return OrderPlacementRuleResult.Block(_translationService.GetResource(
                "Checkout.RecurringPayment.PaymentMethodNotSupported", context.Language.Id));

        return OrderPlacementRuleResult.Allow();
    }
}
