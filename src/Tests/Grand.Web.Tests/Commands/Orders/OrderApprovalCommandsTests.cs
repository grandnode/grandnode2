using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
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
using Grand.Domain.Payments;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Commands.Handler.Orders;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using Microsoft.Extensions.Logging;
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
    private Mock<IOrderService> _orders;
    private Mock<IMediator> _mediator;
    private Mock<ILogger<DecideOrderApprovalCommandHandler>> _logger;
    private Order _order;

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
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<DecideOrderApprovalCommandHandler>>();
        _order = new Order {
            Id = "o1", CustomerId = "buyer", OwnerId = OwnerId, StoreId = StoreId,
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        };
        _orders = new Mock<IOrderService>();
        _orders.Setup(o => o.GetOrderById("o1")).ReturnsAsync(() => _order);
    }



    #region Decide

    private DecideOrderApprovalCommandHandler Decider()
    {
        _customers.Setup(c => c.GetCustomerById("buyer")).ReturnsAsync(Buyer());
        return new DecideOrderApprovalCommandHandler(_approvals.Object, _customers.Object, _messages.Object,
            _orders.Object, _mediator.Object, _logger.Object);
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

    private WithdrawOrderApprovalCommandHandler Withdrawer() => new(_approvals.Object, _orders.Object, _mediator.Object,
        new Mock<ILogger<WithdrawOrderApprovalCommandHandler>>().Object);

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

    #region Orders awaiting approval

    private List<object> Sent()
    {
        var sent = new List<object>();
        _mediator.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((c, _) => sent.Add(c)).ReturnsAsync(true);
        _mediator.Setup(m => m.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((c, _) => sent.Add(c)).Returns(Task.CompletedTask);
        return sent;
    }

    [TestMethod]
    public async Task Approve_MovesOrderToPending_AndNotifiesStoreOwnerOnly()
    {
        var sent = Sent();
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsTrue(result);
        var status = sent.OfType<SetOrderStatusCommand>().Single();
        Assert.AreEqual(OrderStatusSystem.Pending, status.Os);
        Assert.IsFalse(status.NotifyCustomer);
        Assert.IsFalse(status.NotifyStoreOwner);
        Assert.AreSame(_order, status.Order);
        Assert.AreEqual(1, sent.OfType<CheckOrderStatusCommand>().Count());
        var note = sent.OfType<OrderNotificationCommand>().Single();
        Assert.IsFalse(note.NotifyCustomer);
        Assert.IsTrue(note.NotifyStoreOwnerAndVendors);
        Assert.AreEqual("buyer", note.Customer.Id);
        // the order got its "Order placed" note at placement
        Assert.IsFalse(note.AddOrderPlacedNote);
        Assert.AreEqual(0, sent.OfType<ProcessOrderPaidCommand>().Count());
        Assert.AreEqual(0, sent.OfType<CancelOrderCommand>().Count());
        // statuses first, then the e-mails
        Assert.IsTrue(sent.IndexOf(status) < sent.IndexOf(note));
    }

    [TestMethod]
    public async Task Approve_AddsOneOrderApprovedNote_HiddenFromTheCustomer()
    {
        Sent();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(Approval());

        await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        _orders.Verify(o => o.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == _order.Id && n.Note == "Order approved" && !n.DisplayToCustomer)), Times.Once);
    }

    [TestMethod]
    public async Task Reject_AddsNoOrderApprovedNote()
    {
        Sent();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(Approval());

        await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, false), default);

        _orders.Verify(o => o.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task Approve_PaidOrder_ProcessesItAsPaid()
    {
        var sent = Sent();
        _order.PaymentStatusId = PaymentStatus.Paid;
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(Approval());

        await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.AreEqual(1, sent.OfType<ProcessOrderPaidCommand>().Count());
    }

    [TestMethod]
    public async Task Approve_OrderNoLongerAwaiting_Refused()
    {
        var sent = Sent();
        _order.OrderStatusId = (int)OrderStatusSystem.Cancelled;
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.AreEqual(0, sent.Count);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Approve_OrderMissing_Refused()
    {
        _order = null;
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(Approval());

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsFalse(result);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Approve_NotificationFails_DecisionStands()
    {
        Sent();
        _mediator.Setup(m => m.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("smtp"));
        _messages.Setup(m => m.SendOrderApprovalDecidedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>())).ThrowsAsync(new Exception("smtp"));
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Approved, approval.Status);
    }

    [TestMethod]
    public async Task Reject_CancelsOrderWithoutNotifyingAndTellsTheBuyer()
    {
        var sent = Sent();
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, false), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Rejected, approval.Status);
        var cancel = sent.OfType<CancelOrderCommand>().Single();
        Assert.IsFalse(cancel.NotifyCustomer);
        Assert.IsFalse(cancel.NotifyStoreOwner);
        Assert.AreSame(_order, cancel.Order);
        Assert.AreEqual(0, sent.OfType<SetOrderStatusCommand>().Count());
        Assert.AreEqual(0, sent.OfType<OrderNotificationCommand>().Count());
        _messages.Verify(m => m.SendOrderApprovalDecidedMessage(approval, It.IsAny<Customer>(),
            It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Reject_CancelThrows_ApprovalStaysPendingAndFailureIsReported()
    {
        Sent();
        _mediator.Setup(m => m.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, false), default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
        _messages.Verify(m => m.SendOrderApprovalDecidedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Withdraw_CancelThrows_ApprovalStaysPendingAndFailureIsReported()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
    }

    [TestMethod]
    public async Task Approve_SetStatusThrows_ApprovalStaysPending()
    {
        var sent = Sent();
        _mediator.Setup(m => m.Send(It.IsAny<SetOrderStatusCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        _approvals.Verify(a => a.Update(It.IsAny<OrderApproval>()), Times.Never);
        Assert.AreEqual(0, sent.Count);
    }

    [TestMethod]
    public async Task Approve_ProcessPaidThrows_DecisionStandsAndIsLogged()
    {
        Sent();
        _order.PaymentStatusId = PaymentStatus.Paid;
        _mediator.Setup(m => m.Send(It.IsAny<ProcessOrderPaidCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Approved, approval.Status);
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        _messages.Verify(m => m.SendOrderApprovalDecidedMessage(approval, It.IsAny<Customer>(),
            It.IsAny<StoreEntity>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Reject_OrderNoLongerAwaiting_Refused()
    {
        var sent = Sent();
        _order.OrderStatusId = (int)OrderStatusSystem.Pending;
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Decider().Handle(DecideCommand(new Customer { Id = OwnerId }, false), default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.AreEqual(0, sent.Count);
    }

    [TestMethod]
    public async Task Decide_ApproverOfAnotherCompany_CannotTouchTheOrder()
    {
        var sent = Sent();
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);
        var foreign = new Customer { Id = "ownerB", Active = true };

        Assert.IsFalse(await Decider().Handle(DecideCommand(foreign), default));

        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.AreEqual(0, sent.Count);
    }

    [TestMethod]
    public async Task Decide_BuyerApprovingOwnOrder_Refused()
    {
        var sent = Sent();
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        Assert.IsFalse(await Decider().Handle(DecideCommand(Buyer()), default));

        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.AreEqual(0, sent.Count);
    }

    [TestMethod]
    public async Task TwoOrdersOfOneBuyer_AreDecidedIndependently()
    {
        var sent = Sent();
        var order2 = new Order {
            Id = "o2", CustomerId = "buyer", OwnerId = OwnerId, StoreId = StoreId,
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        };
        _orders.Setup(o => o.GetOrderById("o2")).ReturnsAsync(order2);
        var first = Approval(id: "a1");
        var second = Approval(id: "a2");
        second.OrderId = "o2";
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(first);
        _approvals.Setup(a => a.GetById("a2", OwnerId)).ReturnsAsync(second);
        var owner = new Customer { Id = OwnerId, Active = true };

        Assert.IsTrue(await Decider().Handle(DecideCommand(owner, true, id: "a1"), default));

        Assert.AreEqual(OrderApprovalStatus.Approved, first.Status);
        Assert.AreEqual(OrderApprovalStatus.Pending, second.Status);
        Assert.AreEqual((int)OrderStatusSystem.AwaitingApproval, order2.OrderStatusId);
        Assert.IsTrue(sent.OfType<SetOrderStatusCommand>().All(c => c.Order.Id == "o1"));

        Assert.IsTrue(await Decider().Handle(DecideCommand(owner, false, id: "a2"), default));
        Assert.AreEqual(OrderApprovalStatus.Rejected, second.Status);
        Assert.AreEqual("o2", sent.OfType<CancelOrderCommand>().Single().Order.Id);
    }

    [TestMethod]
    public async Task Withdraw_CancelsTheOrderWithoutNotifying()
    {
        var sent = Sent();
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsTrue(result);
        var cancel = sent.OfType<CancelOrderCommand>().Single();
        Assert.IsFalse(cancel.NotifyCustomer);
        Assert.AreSame(_order, cancel.Order);
    }

    [TestMethod]
    public async Task Withdraw_OrderNoLongerAwaiting_Refused()
    {
        var sent = Sent();
        _order.OrderStatusId = (int)OrderStatusSystem.Pending;
        var approval = Approval();
        _approvals.Setup(a => a.GetById("a1", OwnerId)).ReturnsAsync(approval);

        var result = await Withdrawer().Handle(
            new WithdrawOrderApprovalCommand { Customer = Buyer(), Store = Shop(), ApprovalId = "a1" }, default);

        Assert.IsFalse(result);
        Assert.AreEqual(OrderApprovalStatus.Pending, approval.Status);
        Assert.AreEqual(0, sent.Count);
    }

    #endregion
}
