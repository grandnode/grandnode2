using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class OrderApprovalDecisionCommandsTests
{
    private readonly Customer _buyer = new() { Id = "buyer", Email = "buyer@example.com" };
    private readonly Customer _staff = new() { Id = "staff", Email = "staff@example.com" };

    private ApplyOrderApprovalDecisionCommandHandler _apply;
    private OrderApproval _approval;
    private Mock<IOrderApprovalService> _approvals;
    private DecideStoreOrderApprovalCommandHandler _decideStore;
    private Mock<IMediator> _mediator;
    private Mock<ICustomerMessageProviderService> _messages;
    private Order _order;
    private Mock<IOrderService> _orders;

    [TestInitialize]
    public void Init()
    {
        _order = new Order {
            Id = "o1", CustomerId = "buyer", StoreId = "s1", OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        };
        _approval = new OrderApproval {
            Id = "a1", OrderId = "o1", CustomerId = "buyer", StoreId = "s1", Status = OrderApprovalStatus.Pending,
            Decider = OrderApprovalDecider.Store
        };
        _approvals = new Mock<IOrderApprovalService>();
        _approvals.Setup(x => x.GetByOrderId("o1")).ReturnsAsync(_approval);
        _approvals.Setup(x => x.Close(It.IsAny<OrderApproval>(), It.IsAny<OrderApprovalStatus>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((OrderApproval a, OrderApprovalStatus status, string decidedBy, string comment) =>
            {
                a.Status = status;
                a.DecidedByCustomerId = decidedBy;
                a.DecisionComment = comment;
                return true;
            });
        _orders = new Mock<IOrderService>();
        _orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("buyer")).ReturnsAsync(_buyer);
        var stores = new Mock<IStoreService>();
        stores.Setup(x => x.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1" });
        _messages = new Mock<ICustomerMessageProviderService>();
        _mediator = new Mock<IMediator>();

        _apply = new ApplyOrderApprovalDecisionCommandHandler(_approvals.Object, customers.Object, _messages.Object,
            _orders.Object, stores.Object, _mediator.Object,
            new Mock<ILogger<ApplyOrderApprovalDecisionCommandHandler>>().Object);
        _mediator.Setup(x => x.Send(It.IsAny<ApplyOrderApprovalDecisionCommand>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest<bool> command, CancellationToken token) =>
                _apply.Handle((ApplyOrderApprovalDecisionCommand)command, token));
        _decideStore = new DecideStoreOrderApprovalCommandHandler(_approvals.Object, _mediator.Object);
    }

    private ApplyOrderApprovalDecisionCommand Apply(bool approve = true, Customer decidedBy = null)
    {
        return new ApplyOrderApprovalDecisionCommand {
            Approval = _approval, DecidedBy = decidedBy ?? _staff, Approve = approve, Comment = " note ",
            Language = new Language { Id = "en" }
        };
    }

    private DecideStoreOrderApprovalCommand DecideStore(Customer decidedBy = null)
    {
        return new DecideStoreOrderApprovalCommand {
            Order = _order, DecidedBy = decidedBy ?? _staff, Approve = true, Language = new Language { Id = "en" }
        };
    }

    private void NothingWasTouched()
    {
        _approvals.Verify(x => x.Close(It.IsAny<OrderApproval>(), It.IsAny<OrderApprovalStatus>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        OrderWasNotMoved();
    }

    private void OrderWasNotMoved()
    {
        _mediator.Verify(x => x.Send(It.IsAny<SetOrderStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Apply_Approve_MovesTheOrderAndRecordsTheDecision()
    {
        var result = await _apply.Handle(Apply(), default);

        Assert.IsTrue(result);
        Assert.AreEqual(OrderApprovalStatus.Approved, _approval.Status);
        Assert.AreEqual("staff", _approval.DecidedByCustomerId);
        Assert.AreEqual("note", _approval.DecisionComment);
        _mediator.Verify(x => x.Send(It.Is<SetOrderStatusCommand>(c => c.Os == OrderStatusSystem.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Apply_AnotherDecisionWonTheRace_MovesNothingAndSendsNothing()
    {
        _approvals.Setup(x => x.Close(It.IsAny<OrderApproval>(), It.IsAny<OrderApprovalStatus>(), It.IsAny<string>(),
            It.IsAny<string>())).ReturnsAsync(false);

        Assert.IsFalse(await _apply.Handle(Apply(), default));

        OrderWasNotMoved();
        _orders.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
        _messages.Verify(x => x.SendOrderApprovalDecidedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Apply_SavingTheDecisionFails_OrderStaysAwaiting()
    {
        //a failed save must not leave a moved order whose request can never be decided again
        _approvals.Setup(x => x.Close(It.IsAny<OrderApproval>(), It.IsAny<OrderApprovalStatus>(), It.IsAny<string>(),
            It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _apply.Handle(Apply(), default));

        OrderWasNotMoved();
    }

    [TestMethod]
    public async Task Apply_MovingTheOrderFails_ReopensTheRequest()
    {
        _mediator.Setup(x => x.Send(It.IsAny<SetOrderStatusCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Assert.IsFalse(await _apply.Handle(Apply(), default));

        _approvals.Verify(x => x.Reopen(_approval), Times.Once);
        _messages.Verify(x => x.SendOrderApprovalDecidedMessage(It.IsAny<OrderApproval>(), It.IsAny<Customer>(),
            It.IsAny<Store>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
    }

    [TestMethod]
    public async Task Apply_NotPending_ReturnsFalseAndTouchesNothing()
    {
        _approval.Status = OrderApprovalStatus.Withdrawn;

        Assert.IsFalse(await _apply.Handle(Apply(), default));

        NothingWasTouched();
    }

    [TestMethod]
    public async Task Apply_OrderNotAwaiting_ReturnsFalse()
    {
        _order.OrderStatusId = (int)OrderStatusSystem.Cancelled;

        Assert.IsFalse(await _apply.Handle(Apply(), default));

        NothingWasTouched();
    }

    [TestMethod]
    public async Task Apply_DeciderIsBuyer_ReturnsFalse()
    {
        Assert.IsFalse(await _apply.Handle(Apply(decidedBy: _buyer), default));

        NothingWasTouched();
    }

    [TestMethod]
    public async Task Apply_Twice_SecondCallSendsNoSecondMailOrMove()
    {
        Assert.IsTrue(await _apply.Handle(Apply(), default));
        Assert.IsFalse(await _apply.Handle(Apply(), default));

        _mediator.Verify(x => x.Send(It.IsAny<SetOrderStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _messages.Verify(x => x.SendOrderApprovalDecidedMessage(_approval, _buyer, It.IsAny<Store>(),
            It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Apply_GuestBuyer_SendsDecisionToTheBillingEmail()
    {
        _buyer.Email = null;
        _order.BillingAddress = new Address { Email = "guest@example.com" };

        await _apply.Handle(Apply(false), default);

        _messages.Verify(x => x.SendOrderApprovalDecidedMessage(_approval, _buyer, It.IsAny<Store>(),
            It.IsAny<string>(), It.IsAny<DomainHost>(), "guest@example.com"), Times.Once);
    }

    [TestMethod]
    public async Task Apply_Approve_WritesNoteNamingTheDecider()
    {
        await _apply.Handle(Apply(), default);

        _orders.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && n.Note == "Order approved by staff@example.com" && !n.DisplayToCustomer)), Times.Once);
    }

    [TestMethod]
    public async Task Apply_Reject_WritesNoteNamingTheDecider()
    {
        await _apply.Handle(Apply(false), default);

        Assert.AreEqual(OrderApprovalStatus.Rejected, _approval.Status);
        _mediator.Verify(x => x.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _orders.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && n.Note == "Order rejected by staff@example.com" && !n.DisplayToCustomer)), Times.Once);
    }

    [TestMethod]
    public async Task DecideStore_NoRequest_False()
    {
        _approvals.Setup(x => x.GetByOrderId("o1")).ReturnsAsync((OrderApproval)null);

        Assert.IsFalse(await _decideStore.Handle(DecideStore(), default));

        NothingWasTouched();
    }

    [TestMethod]
    public async Task DecideStore_CompanyDecided_False()
    {
        _approval.Decider = OrderApprovalDecider.Company;

        Assert.IsFalse(await _decideStore.Handle(DecideStore(), default));

        NothingWasTouched();
    }

    [TestMethod]
    public async Task DecideStore_StoreDecided_DelegatesToApply()
    {
        Assert.IsTrue(await _decideStore.Handle(DecideStore(), default));

        Assert.AreEqual(OrderApprovalStatus.Approved, _approval.Status);
        Assert.AreEqual("staff", _approval.DecidedByCustomerId);
    }

    [TestMethod]
    public async Task DecideStore_OwnOrder_False()
    {
        Assert.IsFalse(await _decideStore.Handle(DecideStore(_buyer), default));

        NothingWasTouched();
    }
}
