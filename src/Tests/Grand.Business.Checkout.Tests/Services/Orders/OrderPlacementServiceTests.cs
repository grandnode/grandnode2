using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderPlacementServiceTests
{
    private const string RuleFailedText = "cannot be placed right now";
    private const string ApprovalRequiredText = "needs approval";
    private const string BlockedText = "cannot be placed";

    private readonly List<ShoppingCartItem> _cart = [new() { ProductId = "p1" }];

    private readonly Language _language = new() { Id = "pl" };

    private CalculationContext _calculation;
    private Mock<IOrderCalculationService> _calculationService;
    private Mock<ILogger<OrderPlacementService>> _logger;
    private List<IOrderPlacementRuleProvider> _rules;
    private OrderPlacementService _service;

    [TestInitialize]
    public void Init()
    {
        _rules = [];
        var customer = new Customer { Id = "c1" };
        customer.Groups.Add("g1");
        _calculation = new CalculationContext(customer, new Store { Id = "s1" }, new Currency(),
            TaxDisplayType.IncludingTax);
        _calculationService = new Mock<IOrderCalculationService>();
        _calculationService
            .Setup(x => x.GetShoppingCartTotal(_cart, _calculation, It.IsAny<bool?>(), It.IsAny<bool>()))
            .ReturnsAsync(new ShoppingCartTotal(150, 0, [], [], 0, 0));
        _logger = new Mock<ILogger<OrderPlacementService>>();

        var translation = new Mock<ITranslationService>();
        Text(translation, "Checkout.OrderPlacement.RuleFailed", RuleFailedText);
        Text(translation, "Checkout.OrderPlacement.ApprovalRequired", ApprovalRequiredText);
        Text(translation, "Checkout.OrderPlacement.Blocked", BlockedText);

        _service = new OrderPlacementService(_rules, _calculationService.Object, translation.Object, _logger.Object);
    }

    private static void Text(Mock<ITranslationService> translation, string key, string text)
    {
        translation.Setup(x => x.GetResource(key, "pl", It.IsAny<string>(), It.IsAny<bool>())).Returns(text);
    }

    private Mock<IOrderPlacementRuleProvider> Rule(OrderPlacementRuleResult result, int priority = 0,
        IList<string> stores = null, IList<string> groups = null)
    {
        var rule = new Mock<IOrderPlacementRuleProvider>();
        rule.SetupGet(x => x.SystemName).Returns("Rule");
        rule.SetupGet(x => x.Priority).Returns(priority);
        rule.SetupGet(x => x.LimitedToStores).Returns(stores ?? []);
        rule.SetupGet(x => x.LimitedToGroups).Returns(groups ?? []);
        rule.Setup(x => x.Evaluate(It.IsAny<OrderPlacementContext>())).ReturnsAsync(result);
        _rules.Add(rule.Object);
        return rule;
    }

    private Task<OrderPlacementEvaluation> Evaluate()
    {
        return _service.Evaluate(_cart, _calculation, _language, "Payments.CashOnDelivery");
    }

    [TestMethod]
    public async Task Evaluate_NoProviders_Allows()
    {
        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.Allow, result.Decision);
        Assert.AreEqual(0, result.Reasons.Count);
    }

    [TestMethod]
    public async Task Evaluate_AnyRequiresApproval_RequiresApproval()
    {
        Rule(OrderPlacementRuleResult.Allow());
        Rule(OrderPlacementRuleResult.RequireApproval("a"));
        Rule(OrderPlacementRuleResult.Allow());

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.RequireApproval, result.Decision);
        CollectionAssert.AreEqual(new[] { "a" }, result.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_BlockBeatsApproval_ReasonsOnlyFromBlock()
    {
        Rule(OrderPlacementRuleResult.RequireApproval("a"));
        Rule(OrderPlacementRuleResult.Block("b"));

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.Block, result.Decision);
        CollectionAssert.AreEqual(new[] { "b" }, result.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_RunsEveryProvider()
    {
        var first = Rule(OrderPlacementRuleResult.Block("a"));
        var second = Rule(OrderPlacementRuleResult.Block("b"));

        var result = await Evaluate();

        first.Verify(x => x.Evaluate(It.IsAny<OrderPlacementContext>()), Times.Once);
        second.Verify(x => x.Evaluate(It.IsAny<OrderPlacementContext>()), Times.Once);
        CollectionAssert.AreEqual(new[] { "a", "b" }, result.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_ReasonsFollowPriority()
    {
        Rule(OrderPlacementRuleResult.RequireApproval("later"), 20);
        Rule(OrderPlacementRuleResult.RequireApproval("sooner"), 10);

        var result = await Evaluate();

        CollectionAssert.AreEqual(new[] { "sooner", "later" }, result.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_PassesTheOrderBeingPlacedToTheRule()
    {
        OrderPlacementContext seen = null;
        var rule = Rule(OrderPlacementRuleResult.Allow());
        rule.Setup(x => x.Evaluate(It.IsAny<OrderPlacementContext>()))
            .Callback<OrderPlacementContext>(c => seen = c)
            .ReturnsAsync(OrderPlacementRuleResult.Allow());

        await Evaluate();

        Assert.AreSame(_cart, seen.Cart);
        Assert.AreSame(_calculation, seen.Calculation);
        Assert.AreSame(_language, seen.Language);
        Assert.AreEqual("Payments.CashOnDelivery", seen.PaymentMethodSystemName);
    }

    [TestMethod]
    public async Task Evaluate_SkipsProviderLimitedToAnotherStore()
    {
        var other = Rule(OrderPlacementRuleResult.Block("other store"), stores: ["s2"]);
        Rule(OrderPlacementRuleResult.RequireApproval("this store"), stores: ["s1"]);

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.RequireApproval, result.Decision);
        other.Verify(x => x.Evaluate(It.IsAny<OrderPlacementContext>()), Times.Never);
    }

    [TestMethod]
    public async Task Evaluate_SkipsProviderLimitedToAnotherGroup()
    {
        var other = Rule(OrderPlacementRuleResult.Block("other group"), groups: ["g2"]);
        Rule(OrderPlacementRuleResult.RequireApproval("this group"), groups: ["g1"]);

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.RequireApproval, result.Decision);
        other.Verify(x => x.Evaluate(It.IsAny<OrderPlacementContext>()), Times.Never);
    }

    [TestMethod]
    public async Task Evaluate_ThrowingProvider_BlocksWithGenericReasonAndLogs()
    {
        Rule(OrderPlacementRuleResult.RequireApproval("a"));
        var failing = Rule(OrderPlacementRuleResult.Allow());
        failing.Setup(x => x.Evaluate(It.IsAny<OrderPlacementContext>()))
            .ThrowsAsync(new InvalidOperationException("connection string secret"));

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.Block, result.Decision);
        CollectionAssert.AreEqual(new[] { RuleFailedText }, result.Reasons.ToArray());
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task Evaluate_ProviderReturnsNothing_Blocks()
    {
        Rule(null);

        var result = await Evaluate();

        Assert.AreEqual(OrderPlacementDecision.Block, result.Decision);
        CollectionAssert.AreEqual(new[] { RuleFailedText }, result.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_EmptyReason_FallsBackToResource()
    {
        Rule(OrderPlacementRuleResult.RequireApproval("  "));

        var approval = await Evaluate();

        CollectionAssert.AreEqual(new[] { ApprovalRequiredText }, approval.Reasons.ToArray());

        Rule(OrderPlacementRuleResult.Block(null));

        var block = await Evaluate();

        CollectionAssert.AreEqual(new[] { BlockedText }, block.Reasons.ToArray());
    }

    [TestMethod]
    public async Task Evaluate_LongReason_IsTrimmedAndCappedAt500()
    {
        Rule(OrderPlacementRuleResult.RequireApproval("  " + new string('x', 600) + "  "));

        var result = await Evaluate();

        Assert.AreEqual(new string('x', 500), result.Reasons[0]);
    }

    [TestMethod]
    public async Task Evaluate_TwoProvidersAskForTotal_CalculatesOnce()
    {
        var totals = new List<double?>();
        foreach (var _ in new[] { 1, 2 })
        {
            var rule = Rule(OrderPlacementRuleResult.Allow());
            rule.Setup(x => x.Evaluate(It.IsAny<OrderPlacementContext>()))
                .Returns(async (OrderPlacementContext c) =>
                {
                    totals.Add(await c.GetTotal());
                    return OrderPlacementRuleResult.Allow();
                });
        }

        await Evaluate();

        CollectionAssert.AreEqual(new double?[] { 150, 150 }, totals);
        _calculationService.Verify(x => x.GetShoppingCartTotal(_cart, _calculation, It.IsAny<bool?>(),
            It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Evaluate_NoProviderAsksForTotal_DoesNotCalculate()
    {
        Rule(OrderPlacementRuleResult.Allow());

        await Evaluate();

        _calculationService.Verify(x => x.GetShoppingCartTotal(It.IsAny<IList<ShoppingCartItem>>(),
            It.IsAny<CalculationContext>(), It.IsAny<bool?>(), It.IsAny<bool>()), Times.Never);
    }
}
