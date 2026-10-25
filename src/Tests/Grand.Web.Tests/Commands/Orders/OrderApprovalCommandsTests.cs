using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StoreEntity = Grand.Domain.Stores.Store;

namespace Grand.Web.Tests.Commands.Orders;

[TestClass]
public class OrderApprovalCommandsTests
{
    private const string OwnerId = "owner";
    private const string StoreId = "store1";

    private Mock<IOrderApprovalService> _approvals;
    private Mock<ICustomerMessageProviderService> _messages;
    private Mock<ICustomerService> _customers;

    private static IRequestTranslationService Translation()
    {
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        return translation.Object;
    }

    private static Customer Buyer() => new() {
        Id = "buyer", OwnerId = OwnerId, SubAccountRole = SubAccountRole.Buyer, Active = true,
        Email = "buyer@example.com"
    };

    private static StoreEntity Shop() => new() { Id = StoreId };

    private static OrderApproval Approval(OrderApprovalStatus status = OrderApprovalStatus.Pending,
        string id = "a1") => new() {
        Id = id, CustomerId = "buyer", CompanyOwnerId = OwnerId, StoreId = StoreId, Status = status,
        CartFingerprint = "fp", Amount = 150
    };

    [TestInitialize]
    public void Init()
    {
        _approvals = new Mock<IOrderApprovalService>();
        _messages = new Mock<ICustomerMessageProviderService>();
        _customers = new Mock<ICustomerService>();
    }

    #region Submit

    private Mock<IShoppingCartService> _cartService;
    private Mock<IMediator> _mediator;
    private Mock<IProductAttributeFormatter> _attributeFormatter;
    private Currency _currency;
    private List<ShoppingCartItem> _cart;
    private SubmitOrderApprovalCommandHandler _submit;

    private void InitSubmit(bool blocked = true, OrderApproval active = null)
    {
        _currency = new Currency { CurrencyCode = "EUR" };
        var product = new Product { Id = "p1", Name = "Widget", Sku = "W-1" };
        _cart = [
            new ShoppingCartItem {
                Id = "c1", ProductId = "p1", Quantity = 2, ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
                Attributes = [new CustomAttribute { Key = "color", Value = "red" }]
            }
        ];

        _cartService = new Mock<IShoppingCartService>();
        _cartService.Setup(s => s.GetShoppingCart(It.IsAny<Customer>(), StoreId, It.IsAny<ShoppingCartType[]>()))
            .ReturnsAsync(_cart);

        var productService = new Mock<IProductService>();
        productService.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);

        var pricing = new Mock<IPricingService>();
        pricing.Setup(p => p.GetUnitPrice(It.IsAny<ShoppingCartItem>(), product, It.IsAny<Customer>(),
                It.IsAny<StoreEntity>(), It.IsAny<Currency>(), true))
            .ReturnsAsync((55d, 0d, new List<ApplyDiscount>()));
        pricing.Setup(p => p.GetSubTotal(It.IsAny<ShoppingCartItem>(), product, It.IsAny<Customer>(),
                It.IsAny<StoreEntity>(), It.IsAny<Currency>(), true))
            .ReturnsAsync((110d, 0d, new List<ApplyDiscount>()));

        var tax = new Mock<ITaxService>();
        tax.Setup(t => t.GetProductPrice(product, It.IsAny<double>(), true, It.IsAny<Customer>(), It.IsAny<StoreEntity>()))
            .ReturnsAsync((Product _, double price, bool _, Customer _, StoreEntity _) => (price, 0d));

        var formatter = new Mock<IPriceFormatter>();
        formatter.Setup(f => f.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns<double, Currency>((price, c) => $"{price:0.##} {c.CurrencyCode}");

        _attributeFormatter = new Mock<IProductAttributeFormatter>();
        _attributeFormatter.Setup(f => f.FormatAttributes(product, It.IsAny<IList<CustomAttribute>>(),
                It.IsAny<CalculationContext>(), It.IsAny<Language>(), It.IsAny<DomainHost>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync("Color: red");

        _mediator = new Mock<IMediator>();
        _mediator.Setup(m => m.Send(It.IsAny<GetOrderTotals>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderTotalsModel {
                SubTotal = "110 EUR", SubTotalDiscountValue = -10, OrderTotalDiscountValue = -5,
                SelectedShippingMethod = "Ground", Shipping = "20 EUR", Tax = "30 EUR", OrderTotal = "150 EUR"
            });

        var state = new OrderApprovalCheckoutState(true, 100, 150, blocked ? null : Approval(OrderApprovalStatus.Approved));
        _approvals.Setup(a => a.GetCheckoutState(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<CalculationContext>()))
            .ReturnsAsync(state);
        _approvals.Setup(a => a.GetActive("buyer", StoreId)).ReturnsAsync(active);

        _customers.Setup(c => c.GetCustomerById(OwnerId)).ReturnsAsync(
            new Customer { Id = OwnerId, Active = true, Email = "owner@example.com" });

        _submit = new SubmitOrderApprovalCommandHandler(_approvals.Object, _cartService.Object,
            productService.Object, pricing.Object, tax.Object, formatter.Object, _attributeFormatter.Object,
            _customers.Object, _messages.Object, _mediator.Object, Translation());
    }

    private void SetCompany(params Customer[] members)
    {
        _customers.Setup(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), StoreId, OwnerId, It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(), It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Customer>(members, 0, int.MaxValue));
    }

    private SubmitOrderApprovalCommand SubmitCommand(Customer buyer = null)
    {
        buyer ??= Buyer();
        return new SubmitOrderApprovalCommand {
            Customer = buyer, Store = Shop(), Language = new Language { Id = "en" },
            Context = new CalculationContext(buyer, Shop(), _currency, TaxDisplayType.IncludingTax),
            Host = new DomainHost { HostName = "shop.example" }
        };
    }

    private static Customer SubAccount(string id, SubAccountRole role, string email = null, bool active = true,
        bool deleted = false) => new() {
        Id = id, OwnerId = OwnerId, SubAccountRole = role, Active = active, Deleted = deleted,
        StoreId = StoreId, Email = email ?? $"{id}@example.com"
    };

    [TestMethod]
    public async Task Submit_NotBlocked_Refused()
    {
        InitSubmit(false);

        var (success, error) = await _submit.Handle(SubmitCommand(), default);

        Assert.IsFalse(success);
        Assert.AreEqual("OrderApproval.Submit.NotNeeded", error);
        _approvals.Verify(a => a.Insert(It.IsAny<OrderApproval>()), Times.Never);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
        _messages.Verify(m => m.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<Customer>(), It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Submit_Blocked_InsertsPendingWithServerSnapshot()
    {
        InitSubmit();
        SetCompany();
        OrderApproval inserted = null;
        _approvals.Setup(a => a.Insert(It.IsAny<OrderApproval>())).Callback<OrderApproval>(a => inserted = a)
            .Returns(Task.CompletedTask);

        var (success, error) = await _submit.Handle(SubmitCommand(), default);

        Assert.IsTrue(success);
        Assert.AreEqual(string.Empty, error);
        Assert.IsNotNull(inserted);
        Assert.AreEqual(OrderApprovalStatus.Pending, inserted.Status);
        Assert.AreEqual("buyer", inserted.CustomerId);
        Assert.AreEqual(OwnerId, inserted.CompanyOwnerId);
        Assert.AreEqual(StoreId, inserted.StoreId);
        Assert.AreEqual(150, inserted.Amount);
        Assert.AreEqual("EUR", inserted.CurrencyCode);
        Assert.AreEqual(OrderApprovalFingerprint.Compute(_cart), inserted.CartFingerprint);

        var item = inserted.Items.Single();
        Assert.AreEqual("p1", item.ProductId);
        Assert.AreEqual("Widget", item.ProductName);
        Assert.AreEqual("W-1", item.Sku);
        Assert.AreEqual(2, item.Quantity);
        Assert.AreEqual("Color: red", item.AttributeDescription);
        Assert.AreEqual("55 EUR", item.UnitPrice);
        Assert.AreEqual("110 EUR", item.LineTotal);

        Assert.AreEqual("110 EUR", inserted.Summary.SubTotal);
        Assert.AreEqual("-15 EUR", inserted.Summary.Discount);
        Assert.AreEqual("Ground", inserted.Summary.ShippingMethod);
        Assert.AreEqual("20 EUR", inserted.Summary.Shipping);
        Assert.AreEqual("30 EUR", inserted.Summary.Tax);
        Assert.AreEqual("150 EUR", inserted.Summary.Total);
    }

    [TestMethod]
    public async Task Submit_StoresAttributesAsPlainText()
    {
        InitSubmit();
        SetCompany();

        await _submit.Handle(SubmitCommand(), default);

        _attributeFormatter.Verify(f => f.FormatAttributes(It.IsAny<Product>(), It.IsAny<IList<CustomAttribute>>(),
            It.IsAny<CalculationContext>(), It.IsAny<Language>(), It.IsAny<DomainHost>(), ", ", false,
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), false, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Submit_WithdrawsPreviousActive()
    {
        var previous = Approval(OrderApprovalStatus.Approved, "old");
        InitSubmit(active: previous);
        SetCompany();
        var order = new List<string>();
        _approvals.Setup(a => a.Update(previous)).Callback(() => order.Add("withdraw")).Returns(Task.CompletedTask);
        _approvals.Setup(a => a.Insert(It.IsAny<OrderApproval>())).Callback(() => order.Add("insert"))
            .Returns(Task.CompletedTask);

        var (success, _) = await _submit.Handle(SubmitCommand(), default);

        Assert.IsTrue(success);
        Assert.AreEqual(OrderApprovalStatus.Withdrawn, previous.Status);
        CollectionAssert.AreEqual(new[] { "withdraw", "insert" }, order);
    }

    [TestMethod]
    public async Task Submit_NotifiesOwnerAndApproversOnly()
    {
        InitSubmit();
        SetCompany(
            SubAccount("approver", SubAccountRole.Approver),
            SubAccount("buyer2", SubAccountRole.Buyer),
            SubAccount("inactive", SubAccountRole.Approver, active: false),
            SubAccount("deleted", SubAccountRole.Approver, deleted: true),
            SubAccount("noemail", SubAccountRole.Approver, email: " "),
            Buyer());
        var recipients = new List<string>();
        _messages.Setup(m => m.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
                It.IsAny<Customer>(), It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()))
            .Callback<OrderApproval, Customer, Customer, StoreEntity, string, DomainHost>((_, _, approver, _, _, _) =>
                recipients.Add(approver.Id))
            .ReturnsAsync(1);

        await _submit.Handle(SubmitCommand(), default);

        CollectionAssert.AreEquivalent(new[] { OwnerId, "approver" }, recipients);
    }

    [TestMethod]
    public async Task Submit_SendsInTheBuyersLanguageWithTheRequestHost()
    {
        InitSubmit();
        SetCompany();
        var command = SubmitCommand();

        await _submit.Handle(command, default);

        _messages.Verify(m => m.SendOrderApprovalRequestedMessage(It.IsAny<OrderApproval>(), command.Customer,
            It.Is<Customer>(c => c.Id == OwnerId), command.Store, "en", command.Host), Times.Once);
    }

    #endregion

    #region Decide

    private DecideOrderApprovalCommandHandler Decider()
    {
        _customers.Setup(c => c.GetCustomerById("buyer")).ReturnsAsync(Buyer());
        return new DecideOrderApprovalCommandHandler(_approvals.Object, _customers.Object, _messages.Object);
    }

    private static DecideOrderApprovalCommand DecideCommand(Customer approver, bool approve = true,
        string comment = null, string id = "a1") => new() {
        Approver = approver, Store = Shop(), ApprovalId = id, Approve = approve, Comment = comment,
        Language = new Language { Id = "en" }, Host = new DomainHost { HostName = "shop.example" }
    };

    [TestMethod]
    public async Task Decide_Approver_Approves()
    {
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);
        var approver = SubAccount("approver", SubAccountRole.Approver);

        var result = await Decider().Handle(DecideCommand(approver, true, "  ok  "), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Approved, approval.Status);
        Assert.AreEqual("approver", approval.DecidedByCustomerId);
        Assert.AreEqual("ok", approval.DecisionComment);
        Assert.IsNotNull(approval.DecidedOnUtc);
        _approvals.Verify(a => a.Update(approval), Times.Once);
        _messages.Verify(m => m.SendOrderApprovalDecidedMessage(approval, It.Is<Customer>(c => c.Id == "buyer"),
            It.Is<StoreEntity>(s => s.Id == StoreId), "en", It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Decide_Owner_Rejects()
    {
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);
        var owner = new Customer { Id = OwnerId, Active = true };

        var result = await Decider().Handle(DecideCommand(owner, false), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Rejected, approval.Status);
        Assert.AreEqual(OwnerId, approval.DecidedByCustomerId);
        Assert.IsNull(approval.DecisionComment);
        _approvals.Verify(a => a.Update(approval), Times.Once);
    }

    [TestMethod]
    public async Task Decide_Buyer_Refused()
    {
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(Buyer()), default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
        _messages.Verify(m => m.SendOrderApprovalDecidedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Decide_OtherCompany_Refused()
    {
        // the approval is the company's; the stranger's own company id finds nothing
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(Approval());

        var result = await Decider().Handle(DecideCommand(new Customer { Id = "stranger" }), default);

        Assert.IsFalse(result);
        _approvals.Verify(a => a.GetById("a1", "stranger"), Times.Once);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Decide_MissingId_Refused()
    {
        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, id: "nope"), default);

        Assert.IsFalse(result);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    [DataRow(OrderApprovalStatus.Approved)]
    [DataRow(OrderApprovalStatus.Rejected)]
    [DataRow(OrderApprovalStatus.Withdrawn)]
    [DataRow(OrderApprovalStatus.Used)]
    public async Task Decide_NotPending_Refused(OrderApprovalStatus status)
    {
        var approval = Approval(status);
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsFalse(result);
        Assert.AreEqual(status, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Decide_AnotherStore_Refused()
    {
        var approval = Approval();
        approval.StoreId = "store2";
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsFalse(result);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Decide_CommentTruncatedAt1000()
    {
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, false, new string('x', 1500)), default);

        Assert.AreEqual(1000, approval.DecisionComment.Length);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task Decide_BlankComment_IsNull(string comment)
    {
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, true, comment), default);

        Assert.IsNull(approval.DecisionComment);
    }

    #endregion

    #region Withdraw

    private WithdrawOrderApprovalCommandHandler Withdrawer() => new(_approvals.Object);

    [TestMethod]
    [DataRow(OrderApprovalStatus.Pending)]
    [DataRow(OrderApprovalStatus.Approved)]
    public async Task Withdraw_Own_Withdrawn(OrderApprovalStatus status)
    {
        var approval = Approval(status);
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Withdrawn, approval.Status);
        _approvals.Verify(a => a.Update(approval), Times.Once);
    }

    [TestMethod]
    public async Task Withdraw_SomeoneElses_Refused()
    {
        var approval = Approval();
        approval.CustomerId = "colleague";
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    [DataRow(OrderApprovalStatus.Rejected)]
    [DataRow(OrderApprovalStatus.Used)]
    [DataRow(OrderApprovalStatus.Withdrawn)]
    public async Task Withdraw_Finished_Refused(OrderApprovalStatus status)
    {
        var approval = Approval(status);
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsFalse(result);
        Assert.AreEqual(status, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Withdraw_MissingId_Refused()
    {
        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "nope" }, default);

        Assert.IsFalse(result);
    }

    #endregion
}
