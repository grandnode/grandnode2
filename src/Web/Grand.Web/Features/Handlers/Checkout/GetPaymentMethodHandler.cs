using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Tax;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Checkout;

public class GetPaymentMethodHandler : IRequestHandler<GetPaymentMethod, CheckoutPaymentMethodModel>
{
    private readonly ICompanyCreditService _companyCreditService;
    private readonly ICurrencyService _currencyService;
    private readonly ILoyaltyPointsService _loyaltyPointsService;

    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly ILoyaltyPointsCalculationService _loyaltyPointsCalculationService;
    private readonly IPaymentService _paymentService;
    private readonly PaymentSettings _paymentSettings;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITaxService _taxService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderPlacementService _orderPlacementService;
    private readonly IRequestTranslationService _translationService;

    public GetPaymentMethodHandler(ILoyaltyPointsService loyaltyPointsService,
        IOrderCalculationService orderTotalCalculationService,
        ILoyaltyPointsCalculationService loyaltyPointsCalculationService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        IPaymentService paymentService,
        ITaxService taxService,
        IOrderApprovalService orderApprovalService,
        IOrderPlacementService orderPlacementService,
        ICompanyCreditService companyCreditService,
        IRequestTranslationService translationService,
        LoyaltyPointsSettings loyaltyPointsSettings,
        PaymentSettings paymentSettings)
    {
        _loyaltyPointsService = loyaltyPointsService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _loyaltyPointsCalculationService = loyaltyPointsCalculationService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _paymentService = paymentService;
        _taxService = taxService;
        _orderApprovalService = orderApprovalService;
        _orderPlacementService = orderPlacementService;
        _companyCreditService = companyCreditService;
        _translationService = translationService;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _paymentSettings = paymentSettings;
    }

    public async Task<CheckoutPaymentMethodModel> Handle(GetPaymentMethod request, CancellationToken cancellationToken)
    {
        var model = new CheckoutPaymentMethodModel();

        //loyalty points
        if (_loyaltyPointsSettings.Enabled)
        {
            var loyaltyPointsBalance =
                await _loyaltyPointsService.GetLoyaltyPointsBalance(request.Customer.Id, request.Store.Id);
            var loyaltyPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(
                await _loyaltyPointsCalculationService.ConvertLoyaltyPointsToAmount(loyaltyPointsBalance),
                request.Currency);
            if (loyaltyPointsAmount > 0 &&
                _loyaltyPointsCalculationService.CheckMinimumLoyaltyPointsToUseRequirement(loyaltyPointsBalance))
            {
                model.DisplayLoyaltyPoints = true;
                model.LoyaltyPointsAmount = _priceFormatter.FormatPrice(loyaltyPointsAmount, request.Currency);
                model.LoyaltyPointsBalance = loyaltyPointsBalance;
                var shoppingCartTotalBase =
                    (await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart,
                        new CalculationContext(request.Customer, request.Store, request.Currency,
                            request.TaxDisplayType), true)).Total;
                model.LoyaltyPointsEnoughToPayForOrder = shoppingCartTotalBase is 0;
            }
        }

        //filter by country
        var paymentMethods = (await _paymentService
                .LoadActivePaymentMethods(request.Customer, request.Store.Id, request.FilterByCountryId))
            .Where(pm => pm.PaymentMethodType is PaymentMethodType.Standard or PaymentMethodType.Redirection).ToList();
        var availablepaymentMethods = new List<IPaymentProvider>();
        foreach (var pm in paymentMethods)
            if (!await pm.HidePaymentMethod(request.Cart))
                availablepaymentMethods.Add(pm);

        //a blocked order has nothing to pay with; one that needs an approval may only use the payment methods the admin chose
        var evaluation = await _orderPlacementService.Evaluate(request.Cart,
            new CalculationContext(request.Customer, request.Store, request.Currency, request.TaxDisplayType),
            request.Language, null);
        if (evaluation.Decision == OrderPlacementDecision.Block)
        {
            availablepaymentMethods.Clear();
            model.NoPaymentMethodMessage = string.Join(" ", evaluation.Reasons);
        }
        else if (evaluation.Decision == OrderPlacementDecision.RequireApproval)
        {
            availablepaymentMethods = availablepaymentMethods
                .Where(pm => _orderApprovalService.IsPaymentMethodAllowed(pm.SystemName)).ToList();
            if (availablepaymentMethods.Count == 0)
                model.NoPaymentMethodMessage = _translationService.GetResource("Checkout.OrderApproval.NoPaymentMethod");
        }

        model.TradeCreditNotice = await GetTradeCreditNotice(request);

        foreach (var pm in availablepaymentMethods)
        {
            var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel {
                Name = pm.FriendlyName,
                Description = _paymentSettings.ShowPaymentDescriptions ? await pm.Description() : string.Empty,
                PaymentMethodSystemName = pm.SystemName,
                LogoUrl = pm.LogoURL
            };
            //payment method additional fee
            var paymentMethodAdditionalFee =
                await _paymentService.GetAdditionalHandlingFee(request.Cart, pm.SystemName);
            var rate = (await _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee,
                request.TaxDisplayType == TaxDisplayType.IncludingTax, request.Customer, request.Store))
                .paymentPrice;
            if (rate > 0)
                pmModel.Fee = _priceFormatter.FormatPrice(rate, request.Currency);

            model.PaymentMethods.Add(pmModel);
        }

        //find a selected (previously) payment method
        var selectedPaymentMethodSystemName =
            request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPaymentMethod,
                request.Store.Id);
        if (!string.IsNullOrEmpty(selectedPaymentMethodSystemName))
        {
            var paymentMethodToSelect = model.PaymentMethods
                .FirstOrDefault(pm => pm.PaymentMethodSystemName.Equals(selectedPaymentMethodSystemName,
                    StringComparison.OrdinalIgnoreCase));
            if (paymentMethodToSelect != null)
                paymentMethodToSelect.Selected = true;
        }

        //if no option has been selected, do it for the first one
        if (model.PaymentMethods.FirstOrDefault(so => so.Selected) != null) return model;
        {
            var paymentMethodToSelect = model.PaymentMethods.FirstOrDefault();
            if (paymentMethodToSelect != null)
                paymentMethodToSelect.Selected = true;
        }

        return model;
    }

    private async Task<string> GetTradeCreditNotice(GetPaymentMethod request)
    {
        var credit = await _companyCreditService.GetCompanyCredit(request.Customer);
        if (!credit.Enabled)
            return null;

        var available = await _currencyService.ConvertFromPrimaryStoreCurrency(credit.Available, request.Currency);
        var limit = await _currencyService.ConvertFromPrimaryStoreCurrency(credit.CreditLimit!.Value, request.Currency);
        var notice = _translationService.GetResource("Checkout.TradeCredit.Available")
            .Replace("{0}", _priceFormatter.FormatPrice(available, request.Currency))
            .Replace("{1}", _priceFormatter.FormatPrice(limit, request.Currency));

        return credit.HasOverdue
            ? $"{notice} {_translationService.GetResource("Checkout.TradeCredit.Overdue")}"
            : notice;
    }
}