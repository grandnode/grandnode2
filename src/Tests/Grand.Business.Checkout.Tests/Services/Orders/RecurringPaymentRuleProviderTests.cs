using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class RecurringPaymentRuleProviderTests
{
    private Mock<IPaymentService> _paymentService;
    private Mock<IRecurringPaymentService> _recurringPaymentService;
    private RecurringPaymentRuleProvider _rule;
    private double? _total;
    private int _totalCalls;

    [TestInitialize]
    public void Init()
    {
        _total = 30;
        _totalCalls = 0;
        _recurringPaymentService = new Mock<IRecurringPaymentService>();
        _recurringPaymentService.Setup(x => x.HasRecurringProducts(It.IsAny<IEnumerable<ShoppingCartItem>>()))
            .ReturnsAsync(true);
        _paymentService = new Mock<IPaymentService>();
        _paymentService.Setup(x => x.SupportRecurringPayment("Payments.Card")).ReturnsAsync(true);
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource("Checkout.RecurringPayment.NothingToPay", "pl", It.IsAny<string>(),
            It.IsAny<bool>())).Returns("Nothing to pay");
        translation.Setup(x => x.GetResource("Checkout.RecurringPayment.PaymentMethodNotSupported", "pl",
            It.IsAny<string>(), It.IsAny<bool>())).Returns("Method cannot charge in cycles");

        _rule = new RecurringPaymentRuleProvider(_recurringPaymentService.Object, _paymentService.Object,
            translation.Object);
    }

    private OrderPlacementContext Context(string paymentMethod)
    {
        var calculation = new CalculationContext(new Customer { Id = "buyer1" }, new Store { Id = "s1" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);
        return new OrderPlacementContext([new ShoppingCartItem { ProductId = "p1" }], calculation,
            new Language { Id = "pl" }, paymentMethod, () =>
            {
                _totalCalls++;
                return Task.FromResult(_total);
            });
    }

    [TestMethod]
    public async Task Evaluate_StandardCart_AllowsWithoutAskingForTotal()
    {
        _recurringPaymentService.Setup(x => x.HasRecurringProducts(It.IsAny<IEnumerable<ShoppingCartItem>>()))
            .ReturnsAsync(false);

        var result = await _rule.Evaluate(Context("Payments.CashOnDelivery"));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
        Assert.AreEqual(0, _totalCalls);
    }

    [TestMethod]
    public async Task Evaluate_RecurringCartWithCapableMethod_Allows()
    {
        var result = await _rule.Evaluate(Context("Payments.Card"));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }

    [TestMethod]
    public async Task Evaluate_RecurringCartWithMethodWithoutRecurringSupport_Blocks()
    {
        //a method chosen in a stale tab or posted by hand
        var result = await _rule.Evaluate(Context("Payments.CashOnDelivery"));

        Assert.AreEqual(OrderPlacementDecision.Block, result.Decision);
        Assert.AreEqual("Method cannot charge in cycles", result.Reason);
    }

    [TestMethod]
    public async Task Evaluate_RecurringCartBeforeMethodIsChosen_Allows()
    {
        var result = await _rule.Evaluate(Context(null));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
        _paymentService.Verify(x => x.SupportRecurringPayment(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Evaluate_RecurringCartWithZeroTotal_Blocks()
    {
        //a full discount, a gift voucher or loyalty points: the payment step is skipped and the method cleared
        _total = 0;

        var result = await _rule.Evaluate(Context(null));

        Assert.AreEqual(OrderPlacementDecision.Block, result.Decision);
        Assert.AreEqual("Nothing to pay", result.Reason);
    }

    [TestMethod]
    public async Task Evaluate_RecurringCartBeforeShippingIsChosen_Allows()
    {
        _total = null;

        var result = await _rule.Evaluate(Context(null));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }
}
