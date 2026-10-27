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
        OrderId = "o1", Amount = 150
    };

    private static Customer SubAccount(string id, SubAccountRole role, string email = null, bool active = true,
        bool deleted = false) => new() {
        Id = id, OwnerId = OwnerId, SubAccountRole = role, Active = active, Deleted = deleted,
        StoreId = StoreId, Email = email ?? $"{id}@example.com"
    };

    [TestInitialize]
    public void Init()
    {
        _approvals = new Mock<IOrderApprovalService>();
        _messages = new Mock<ICustomerMessageProviderService>();
        _customers = new Mock<ICustomerService>();
    }

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
    public async Task Withdraw_Own_Withdrawn()
    {
        var approval = Approval();
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
