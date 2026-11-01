using Grand.Business.Core.Enums.Checkout;
using Grand.Domain.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Checkout;
using Grand.Web.Features.Models.Checkout;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Checkout;

[TestClass]
public class CheckoutOrderApprovalHandlersTests
{
    private Mock<IOrderApprovalService> _approval;
    private IList<ShoppingCartItem> _cart;
    private Mock<ICompanyCreditService> _credit;
    private Currency _currency;
    private Customer _customer;
    private readonly Language _language = new() { Id = "pl" };
    private Mock<IPaymentService> _paymentService;
    private Mock<IOrderPlacementService> _placement;
    private Grand.Domain.Stores.Store _store;
    private Mock<IRequestTranslationService> _translation;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer", OwnerId = "owner" };
        _store = new Grand.Domain.Stores.Store { Id = "s1" };
        _currency = new Currency { Id = "c1", CurrencyCode = "EUR" };
        _cart = new List<ShoppingCartItem> { new() { ProductId = "p1", Quantity = 1 } };
        _approval = new Mock<IOrderApprovalService>();
        _paymentService = new Mock<IPaymentService>();
        _credit = new Mock<ICompanyCreditService>();
        _credit.Setup(c => c.GetCompanyCredit(_customer)).ReturnsAsync(CompanyCredit.None("owner"));
        _translation = new Mock<IRequestTranslationService>();
        _translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        _translation.Setup(t => t.GetResource("Checkout.OrderApproval.WillBeSentGeneric")).Returns("Goes for approval.");
        _translation.Setup(t => t.GetResource("Checkout.TradeCredit.Available")).Returns("Available {0} of {1}.");
        _translation.Setup(t => t.GetResource("Checkout.TradeCredit.Overdue")).Returns("Overdue.");
        _placement = new Mock<IOrderPlacementService>();
    }

    private void Evaluates(OrderPlacementDecision decision, params string[] reasons) =>
        _placement.Setup(a => a.Evaluate(_cart, It.Is<CalculationContext>(c => c.Customer == _customer), _language,
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(decision, reasons));

    private void State(bool required) =>
        Evaluates(required ? OrderPlacementDecision.RequireApproval : OrderPlacementDecision.Allow,
            required ? ["Over the limit."] : []);

    private static Mock<IPaymentProvider> Provider(string systemName)
    {
        var provider = new Mock<IPaymentProvider>();
        provider.Setup(p => p.SystemName).Returns(systemName);
        provider.Setup(p => p.FriendlyName).Returns(systemName);
        provider.Setup(p => p.PaymentMethodType).Returns(PaymentMethodType.Standard);
        provider.Setup(p => p.HidePaymentMethod(It.IsAny<IList<ShoppingCartItem>>())).ReturnsAsync(false);
        provider.Setup(p => p.Description()).ReturnsAsync(string.Empty);
        return provider;
    }

    private GetPaymentMethodHandler PaymentHandler(params string[] methods)
    {
        _paymentService.Setup(p => p.LoadActivePaymentMethods(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(methods.Select(m => Provider(m).Object).ToList());
        var tax = new Mock<ITaxService>();
        tax.Setup(t => t.GetPaymentMethodAdditionalFee(It.IsAny<double>(), It.IsAny<bool>(), It.IsAny<Customer>(),
            It.IsAny<Grand.Domain.Stores.Store>())).ReturnsAsync((0d, 0d));
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(p => p.FormatPrice(It.IsAny<double>(), _currency)).Returns<double, Currency>((p, _) => $"{p:0} EUR");
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), _currency))
            .ReturnsAsync((double v, Currency _) => v * 2);
        return new GetPaymentMethodHandler(new Mock<ILoyaltyPointsService>().Object,
            new Mock<IOrderCalculationService>().Object, new Mock<ILoyaltyPointsCalculationService>().Object,
            currencyService.Object, priceFormatter.Object, _paymentService.Object,
            tax.Object, _approval.Object, _placement.Object, _credit.Object, _translation.Object,
            new LoyaltyPointsSettings(),
            new PaymentSettings());
    }

    private GetPaymentMethod PaymentRequest() => new() {
        Customer = _customer, Store = _store, Currency = _currency, TaxDisplayType = TaxDisplayType.IncludingTax,
        Cart = _cart, Language = _language
    };

    [TestMethod]
    public async Task GetPaymentMethod_Blocked_NoMethodsAndReasonsInMessage()
    {
        //no method is chosen yet at this step
        _placement.Setup(a => a.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(OrderPlacementDecision.Block, ["Blocked customer.", "Banned product."]));
        _approval.Setup(a => a.IsPaymentMethodAllowed("Payments.CashOnDelivery")).Returns(true);

        var model = await PaymentHandler("Payments.Card", "Payments.CashOnDelivery")
            .Handle(PaymentRequest(), CancellationToken.None);

        Assert.IsEmpty(model.PaymentMethods);
        Assert.AreEqual("Blocked customer. Banned product.", model.NoPaymentMethodMessage);
    }

    [TestMethod]
    public async Task PaymentMethods_ApprovalRequired_KeepsOnlyAllowedMethods()
    {
        State(true);
        _approval.Setup(a => a.IsPaymentMethodAllowed("Payments.CashOnDelivery")).Returns(true);

        var model = await PaymentHandler("Payments.Card", "Payments.CashOnDelivery")
            .Handle(PaymentRequest(), CancellationToken.None);

        Assert.HasCount(1, model.PaymentMethods);
        Assert.AreEqual("Payments.CashOnDelivery", model.PaymentMethods[0].PaymentMethodSystemName);
        Assert.IsTrue(model.PaymentMethods[0].Selected);
        Assert.IsNull(model.NoPaymentMethodMessage);
    }

    [TestMethod]
    public async Task PaymentMethods_ApprovalRequiredAndNoneAllowed_ShowsTheMessage()
    {
        State(true);

        var model = await PaymentHandler("Payments.Card").Handle(PaymentRequest(), CancellationToken.None);

        Assert.IsEmpty(model.PaymentMethods);
        Assert.AreEqual("Checkout.OrderApproval.NoPaymentMethod", model.NoPaymentMethodMessage);
    }

    [TestMethod]
    public async Task PaymentMethods_ApprovalNotRequired_KeepsEveryMethod()
    {
        State(false);

        var model = await PaymentHandler("Payments.Card", "Payments.CashOnDelivery")
            .Handle(PaymentRequest(), CancellationToken.None);

        Assert.HasCount(2, model.PaymentMethods);
        Assert.IsNull(model.NoPaymentMethodMessage);
        _approval.Verify(a => a.IsPaymentMethodAllowed(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PaymentMethods_NoTradeCredit_ShowsNoNotice()
    {
        State(false);

        var model = await PaymentHandler("Payments.Card").Handle(PaymentRequest(), CancellationToken.None);

        Assert.IsNull(model.TradeCreditNotice);
    }

    [TestMethod]
    public async Task PaymentMethods_TradeCredit_ShowsAvailableAndLimitInTheWorkingCurrency()
    {
        State(false);
        _credit.Setup(c => c.GetCompanyCredit(_customer))
            .ReturnsAsync(new CompanyCredit("owner", 1000, 14, 400, false));

        var model = await PaymentHandler("Payments.Card").Handle(PaymentRequest(), CancellationToken.None);

        Assert.AreEqual("Available 1200 EUR of 2000 EUR.", model.TradeCreditNotice);
    }

    [TestMethod]
    public async Task PaymentMethods_TradeCreditOverdue_AddsTheOverdueSentence()
    {
        State(false);
        _credit.Setup(c => c.GetCompanyCredit(_customer))
            .ReturnsAsync(new CompanyCredit("owner", 1000, 14, 400, true));

        var model = await PaymentHandler("Payments.Card").Handle(PaymentRequest(), CancellationToken.None);

        Assert.AreEqual("Available 1200 EUR of 2000 EUR. Overdue.", model.TradeCreditNotice);
    }

    private GetConfirmOrderHandler ConfirmHandler()
    {
        return new GetConfirmOrderHandler(new Mock<ICountryService>().Object, _paymentService.Object,
            new Mock<IMediator>().Object, _placement.Object, _translation.Object, new OrderSettings(),
            new ShippingSettings());
    }

    private GetConfirmOrder ConfirmRequest() => new() {
        Customer = _customer, Store = _store, Currency = _currency, TaxDisplayType = TaxDisplayType.IncludingTax,
        Cart = _cart, Language = _language
    };

    [TestMethod]
    public async Task GetConfirmOrder_ApprovalRequired_MessageHoldsGenericLineAndReasons()
    {
        Evaluates(OrderPlacementDecision.RequireApproval, "Over the limit.", "Restricted goods.");

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.IsTrue(model.OrderApprovalRequired);
        Assert.AreEqual("Goes for approval. Over the limit. Restricted goods.", model.OrderApprovalMessage);
        Assert.AreEqual("Checkout.OrderApproval.SendButton", model.OrderApprovalButtonText);
        Assert.IsFalse(model.OrderPlacementBlocked);
    }

    [TestMethod]
    public async Task GetConfirmOrder_Blocked_SetsBlockedFlagAndMessage()
    {
        Evaluates(OrderPlacementDecision.Block, "Blocked customer.", "Banned product.");

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.IsTrue(model.OrderPlacementBlocked);
        Assert.AreEqual("Blocked customer. Banned product.", model.OrderPlacementBlockedMessage);
        Assert.IsFalse(model.OrderApprovalRequired);
    }

    [TestMethod]
    public async Task GetConfirmOrder_PassesTheSelectedPaymentMethodToTheRules()
    {
        SelectPaymentMethod("Payments.Card", PurchaseOrderNumberMode.Disabled);
        _placement.Setup(a => a.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, "Payments.Card",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(OrderPlacementDecision.Block, ["Not with this method."]));

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.AreEqual("Not with this method.", model.OrderPlacementBlockedMessage);
    }

    [TestMethod]
    public async Task GetConfirmOrder_PassesItsTokenToTheRules()
    {
        using var source = new CancellationTokenSource();
        SelectPaymentMethod("Payments.Card", PurchaseOrderNumberMode.Disabled);
        State(false);

        await ConfirmHandler().Handle(ConfirmRequest(), source.Token);

        _placement.Verify(a => a.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, "Payments.Card",
            source.Token), Times.Once);
    }

    [TestMethod]
    public async Task GetPaymentMethod_PassesItsTokenToTheRules()
    {
        using var source = new CancellationTokenSource();
        State(false);

        await PaymentHandler("Payments.Card").Handle(PaymentRequest(), source.Token);

        _placement.Verify(a => a.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, null, source.Token),
            Times.Once);
    }

    private void SelectPaymentMethod(string systemName, PurchaseOrderNumberMode mode)
    {
        _customer.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.SelectedPaymentMethod, Value = systemName, StoreId = _store.Id
        });
        var provider = Provider(systemName);
        provider.Setup(p => p.PurchaseOrderNumberMode).Returns(mode);
        _paymentService.Setup(p => p.LoadPaymentMethodBySystemName(systemName)).Returns(provider.Object);
    }

    [TestMethod]
    public async Task Confirm_SelectedMethodAsksForThePurchaseOrderNumber_CarriesItsMode()
    {
        State(false);
        SelectPaymentMethod("Payments.OnAccount", PurchaseOrderNumberMode.Required);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.AreEqual(PurchaseOrderNumberMode.Required, model.PurchaseOrderNumberMode);
    }

    [TestMethod]
    public async Task Confirm_SelectedMethodDoesNotAsk_NoPurchaseOrderNumber()
    {
        State(false);
        SelectPaymentMethod("Payments.StripeCheckout", PurchaseOrderNumberMode.Disabled);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.AreEqual(PurchaseOrderNumberMode.Disabled, model.PurchaseOrderNumberMode);
    }

    [TestMethod]
    public async Task Confirm_NoMethodSelected_NoPurchaseOrderNumber()
    {
        State(false);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.AreEqual(PurchaseOrderNumberMode.Disabled, model.PurchaseOrderNumberMode);
    }

    [TestMethod]
    public async Task Confirm_ApprovalNotRequired_LeavesTheStateEmpty()
    {
        State(false);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.IsFalse(model.OrderApprovalRequired);
        Assert.IsNull(model.OrderApprovalMessage);
        Assert.IsNull(model.OrderApprovalButtonText);
    }
}
