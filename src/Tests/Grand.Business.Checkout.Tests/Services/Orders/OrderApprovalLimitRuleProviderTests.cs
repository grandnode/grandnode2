using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderApprovalLimitRuleProviderTests
{
    private readonly Currency _currency = new() { CurrencyCode = "PLN" };

    private CustomerSettings _customerSettings;
    private OrderApprovalLimitRuleProvider _rule;
    private double? _total;
    private int _totalCalls;
    private Mock<ITranslationService> _translation;

    [TestInitialize]
    public void Init()
    {
        _customerSettings = new CustomerSettings { HideSubAccountsTab = false };
        _total = 150;
        _totalCalls = 0;

        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.ConvertFromPrimaryStoreCurrency(100, _currency)).ReturnsAsync(400);
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(400, _currency)).Returns("400 zl");
        _translation = new Mock<ITranslationService>();
        Text("The order total exceeds your order limit of {0}.");

        _rule = new OrderApprovalLimitRuleProvider(_customerSettings, currencyService.Object, priceFormatter.Object,
            _translation.Object);
    }

    private void Text(string text)
    {
        _translation.Setup(x => x.GetResource("Checkout.OrderApproval.LimitExceeded", "pl", It.IsAny<string>(),
            It.IsAny<bool>())).Returns(text);
    }

    private static Customer Buyer(double? limit = 100, SubAccountRole role = SubAccountRole.Buyer,
        string ownerId = "owner1")
    {
        return new Customer { Id = "buyer1", OwnerId = ownerId, SubAccountRole = role, OrderApprovalLimit = limit };
    }

    private OrderPlacementContext Context(Customer customer)
    {
        var calculation = new CalculationContext(customer, new Store { Id = "s1" }, _currency,
            TaxDisplayType.IncludingTax);
        return new OrderPlacementContext([], calculation, new Language { Id = "pl" }, null, () =>
        {
            _totalCalls++;
            return Task.FromResult(_total);
        });
    }

    [TestMethod]
    public async Task Evaluate_RetailCustomer_NeverAsksForTotal()
    {
        var result = await _rule.Evaluate(Context(Buyer(100, ownerId: null)));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
        Assert.AreEqual(0, _totalCalls);
    }

    [TestMethod]
    public async Task Evaluate_NoLimit_Allows()
    {
        var result = await _rule.Evaluate(Context(Buyer(null)));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }

    [TestMethod]
    public async Task Evaluate_Approver_Allows()
    {
        var result = await _rule.Evaluate(Context(Buyer(100, SubAccountRole.Approver)));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }

    [TestMethod]
    public async Task Evaluate_FeatureOff_Allows()
    {
        _customerSettings.HideSubAccountsTab = true;

        var result = await _rule.Evaluate(Context(Buyer()));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
        Assert.AreEqual(0, _totalCalls);
    }

    [TestMethod]
    public async Task Evaluate_TotalUnknown_Allows()
    {
        _total = null;

        var result = await _rule.Evaluate(Context(Buyer()));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }

    [TestMethod]
    public async Task Evaluate_TotalAtLimit_Allows()
    {
        _total = 100;

        var result = await _rule.Evaluate(Context(Buyer()));

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
    }

    [TestMethod]
    public async Task Evaluate_OverLimit_ReasonCarriesFormattedLimit()
    {
        var result = await _rule.Evaluate(Context(Buyer()));

        Assert.AreEqual(OrderPlacementDecision.RequireApproval, result.Decision);
        Assert.AreEqual("The order total exceeds your order limit of 400 zl.", result.Reason);
    }

    [TestMethod]
    public async Task Evaluate_BrokenPlaceholderInResource_ReturnsTextUnformatted()
    {
        Text("Over the limit {oops");

        var result = await _rule.Evaluate(Context(Buyer()));

        Assert.AreEqual(OrderPlacementDecision.RequireApproval, result.Decision);
        Assert.AreEqual("Over the limit {oops", result.Reason);
    }
}
