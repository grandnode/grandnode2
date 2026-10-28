using Grand.Business.Core.Enums.Checkout;
using Grand.Domain.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
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
    private Currency _currency;
    private Customer _customer;
    private Mock<IPaymentService> _paymentService;
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
        _translation = new Mock<IRequestTranslationService>();
        _translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        _translation.Setup(t => t.GetResource("Checkout.OrderApproval.WillBeSent")).Returns("Over {0} goes for approval");
    }

    private void State(bool required, double? limit = 100) =>
        _approval.Setup(a => a.RequiresApproval(_cart, It.Is<CalculationContext>(c => c.Customer == _customer)))
            .ReturnsAsync(new OrderApprovalCheckoutState(required, limit, 500));

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
        return new GetPaymentMethodHandler(new Mock<ILoyaltyPointsService>().Object,
            new Mock<IOrderCalculationService>().Object, new Mock<ILoyaltyPointsCalculationService>().Object,
            new Mock<ICurrencyService>().Object, new Mock<IPriceFormatter>().Object, _paymentService.Object,
            tax.Object, _approval.Object, _translation.Object, new LoyaltyPointsSettings(), new PaymentSettings());
    }

    private GetPaymentMethod PaymentRequest() => new() {
        Customer = _customer, Store = _store, Currency = _currency, TaxDisplayType = TaxDisplayType.IncludingTax,
        Cart = _cart
    };

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

    private GetConfirmOrderHandler ConfirmHandler()
    {
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(p => p.FormatPrice(It.IsAny<double>(), _currency)).Returns<double, Currency>((p, _) => $"{p:0} EUR");
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(c => c.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), _currency))
            .ReturnsAsync((double v, Currency _) => v * 2);
        return new GetConfirmOrderHandler(new Mock<ICountryService>().Object, _paymentService.Object,
            new Mock<IMediator>().Object, _approval.Object, currencyService.Object, priceFormatter.Object,
            _translation.Object, new OrderSettings(), new ShippingSettings());
    }

    private GetConfirmOrder ConfirmRequest() => new() {
        Customer = _customer, Store = _store, Currency = _currency, TaxDisplayType = TaxDisplayType.IncludingTax,
        Cart = _cart
    };

    [TestMethod]
    public async Task Confirm_ApprovalRequired_CarriesTheMessageWithTheFormattedLimitAndTheButtonText()
    {
        State(true, 100);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.IsTrue(model.OrderApprovalRequired);
        Assert.AreEqual("Over 200 EUR goes for approval", model.OrderApprovalMessage);
        Assert.AreEqual("Checkout.OrderApproval.SendButton", model.OrderApprovalButtonText);
    }

    [TestMethod]
    public async Task Confirm_BrokenPlaceholderInTheEditedText_StillRendersTheText()
    {
        _translation.Setup(t => t.GetResource("Checkout.OrderApproval.WillBeSent")).Returns("Over {0 goes");
        State(true, 100);

        var model = await ConfirmHandler().Handle(ConfirmRequest(), CancellationToken.None);

        Assert.IsTrue(model.OrderApprovalRequired);
        Assert.AreEqual("Over {0 goes", model.OrderApprovalMessage);
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
