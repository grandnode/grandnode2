using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class RecordRecurringPaymentCommandHandlersTests
{
    private Mock<IRecurringPaymentService> _service;
    private Mock<IPaymentService> _paymentService;
    private Mock<IOrderService> _orderService;
    private Mock<IMediator> _mediator;

    [TestInitialize]
    public void Init()
    {
        _service = new Mock<IRecurringPaymentService>();
        _paymentService = new Mock<IPaymentService>();
        _orderService = new Mock<IOrderService>();
        _mediator = new Mock<IMediator>();
    }

    private RecordRecurringPaymentFailureCommandHandler FailureHandler()
    {
        return new RecordRecurringPaymentFailureCommandHandler(_service.Object, _paymentService.Object,
            _orderService.Object, new Mock<ILogger<RecordRecurringPaymentFailureCommandHandler>>().Object);
    }

    private static RecordRecurringPaymentFailureCommand FailureCommand()
    {
        return new RecordRecurringPaymentFailureCommand {
            RecurringPaymentId = "rp1", ProviderReference = "sub_1", CycleReference = "in_2", Reason = "declined"
        };
    }

    [TestMethod]
    public async Task FailureCommand_PassesTheReportToTheService()
    {
        _service.Setup(x => x.RecordFailure("rp1", "in_2", "declined")).ReturnsAsync(true);

        var result = await FailureHandler().Handle(FailureCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _service.Verify(x => x.RecordFailure("rp1", "in_2", "declined"), Times.Once);
        _paymentService.Verify(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [TestMethod]
    [DataRow(RecurringPaymentStatus.Cancelled)]
    [DataRow(RecurringPaymentStatus.Completed)]
    public async Task FailureCommand_OnAnEndedPayment_AsksTheProviderToStopWithTheReportedReference(
        RecurringPaymentStatus status)
    {
        // cancelled while pending, so the shop never learnt the gateway's reference
        _service.Setup(x => x.GetRecurringPaymentById("rp1")).ReturnsAsync(new RecurringPayment {
            Id = "rp1", InitialOrderId = "o1", Status = status
        });
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        Assert.IsFalse(await FailureHandler().Handle(FailureCommand(), CancellationToken.None));

        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r =>
            r.Id == "rp1" && r.ProviderReference == "sub_1")), Times.Once);
    }

    [TestMethod]
    public async Task FailureCommand_OnAnEndedPaymentAndProviderRefuses_NotesItOnTheInitialOrder()
    {
        _service.Setup(x => x.GetRecurringPaymentById("rp1")).ReturnsAsync(new RecurringPayment {
            Id = "rp1", InitialOrderId = "o1", Status = RecurringPaymentStatus.Cancelled, ProviderReference = "sub_1"
        });
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no way");
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>())).ReturnsAsync(refusal);

        await FailureHandler().Handle(FailureCommand(), CancellationToken.None);

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.OrderId == "o1" &&
            n.Note == "The recurring payment is cancelled, but the payment gateway did not confirm it stopped charging: no way")),
            Times.Once);
    }

    [TestMethod]
    public async Task FailureCommand_ForACyclePaidAlreadyOrAnActivePayment_DoesNotAskTheProviderToStop()
    {
        var cancelled = new RecurringPayment { Id = "rp1", Status = RecurringPaymentStatus.Cancelled };
        cancelled.Cycles.Add(new RecurringPaymentCycle { CycleNumber = 2, ProviderReference = "in_2" });
        _service.Setup(x => x.GetRecurringPaymentById("rp1")).ReturnsAsync(cancelled);
        await FailureHandler().Handle(FailureCommand(), CancellationToken.None);

        _service.Setup(x => x.GetRecurringPaymentById("rp1")).ReturnsAsync(new RecurringPayment {
            Id = "rp1", Status = RecurringPaymentStatus.PastDue
        });
        await FailureHandler().Handle(FailureCommand(), CancellationToken.None);

        _paymentService.Verify(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [TestMethod]
    public async Task FailureCommand_WithoutRecurringPaymentId_Throws()
    {
        var handler = FailureHandler();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            handler.Handle(new RecordRecurringPaymentFailureCommand { RecurringPaymentId = "" }, CancellationToken.None));
    }

    [TestMethod]
    public async Task EndedCommand_PassesTheReportToTheService()
    {
        _service.Setup(x => x.RecordEnded("rp1")).ReturnsAsync(true);
        var handler = new RecordRecurringPaymentEndedCommandHandler(_service.Object);

        var result = await handler.Handle(new RecordRecurringPaymentEndedCommand { RecurringPaymentId = "rp1" },
            CancellationToken.None);

        Assert.IsTrue(result);
        _service.Verify(x => x.RecordEnded("rp1"), Times.Once);
    }

    [TestMethod]
    public async Task EndedCommand_WithoutRecurringPaymentId_Throws()
    {
        var handler = new RecordRecurringPaymentEndedCommandHandler(_service.Object);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            handler.Handle(new RecordRecurringPaymentEndedCommand { RecurringPaymentId = "" }, CancellationToken.None));
    }

    private RecordRecurringPaymentCycleCommandHandler CycleHandler()
    {
        return new RecordRecurringPaymentCycleCommandHandler(_service.Object, _paymentService.Object,
            _orderService.Object, _mediator.Object, new Mock<ILogger<RecordRecurringPaymentCycleCommandHandler>>().Object);
    }

    private static RecordRecurringPaymentCycleCommand CycleCommand()
    {
        return new RecordRecurringPaymentCycleCommand {
            RecurringPaymentId = "rp1", ProviderReference = "sub_1", CycleReference = "in_2", Amount = 10,
            PaidOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private void SetupClaim(int cycleNumber, RecurringPaymentStatus status = RecurringPaymentStatus.Active,
        bool completed = false)
    {
        var claim = new RecurringCycleClaim {
            RecurringPayment = new RecurringPayment {
                Id = "rp1", InitialOrderId = "o1", PaymentMethodSystemName = "Payments.Test", Status = status
            },
            Cycle = new RecurringPaymentCycle { CycleNumber = cycleNumber, OrderId = "o2" },
            Completed = completed
        };
        _service.Setup(x => x.ClaimCycle("rp1", "sub_1", "in_2", 10, It.IsAny<DateTime>(), null)).ReturnsAsync(claim);
    }

    [TestMethod]
    public async Task CycleCommand_AlreadyRecorded_DoesNothing()
    {
        _service.Setup(x => x.ClaimCycle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<double>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>())).ReturnsAsync((RecurringCycleClaim)null);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsFalse(result);
        _mediator.Verify(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_FirstCycle_PlacesNoOrder()
    {
        SetupClaim(1);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _mediator.Verify(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_LaterCycle_PlacesTheCycleOrder()
    {
        SetupClaim(2);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _mediator.Verify(x => x.Send(It.Is<PlaceRecurringCycleOrderCommand>(c =>
            c.RecurringPayment.Id == "rp1" && c.Cycle.CycleNumber == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_OrderCannotBeCreated_NotesItOnTheInitialOrderAndThrows()
    {
        SetupClaim(2);
        _mediator.Setup(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && !n.DisplayToCustomer && n.Note ==
                "Cycle 2 was paid at the payment gateway (in_2), but its order o2 could not be placed or marked paid: boom")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_Completed_AsksTheProviderToStop()
    {
        SetupClaim(3, RecurringPaymentStatus.Completed, completed: true);
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r => r.Id == "rp1")), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_FirstCycleClaimedAfterCancel_AsksTheProviderToStop()
    {
        // a payment cancelled before its first cycle was reported is still charged once at the gateway
        SetupClaim(1, RecurringPaymentStatus.Cancelled);
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        Assert.IsTrue(await CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        _mediator.Verify(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r => r.Id == "rp1")), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.OrderId == "o1" && !n.DisplayToCustomer &&
            n.Note == "Cycle 1 was charged at the payment gateway (in_2) after the recurring payment was cancelled: check whether the customer must be refunded")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_ChargedAfterCompleted_NotesItForARefund()
    {
        SetupClaim(4, RecurringPaymentStatus.Completed);
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        Assert.IsTrue(await CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.Note == "Cycle 4 was charged at the payment gateway (in_2) after the recurring payment was completed: check whether the customer must be refunded")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_NotCompleted_DoesNotAskTheProviderToStop()
    {
        SetupClaim(3);

        await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        _paymentService.Verify(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_CompletedAndProviderRefuses_NotesItOnTheInitialOrder()
    {
        SetupClaim(3, RecurringPaymentStatus.Completed);
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no way");
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>())).ReturnsAsync(refusal);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && !n.DisplayToCustomer &&
            n.Note == "The recurring payment is completed, but the payment gateway did not confirm it stopped charging: no way")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_NoteCannotBeWritten_StillThrowsTheOriginalException()
    {
        SetupClaim(2);
        _mediator.Setup(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _orderService.Setup(x => x.InsertOrderNote(It.IsAny<OrderNote>())).ThrowsAsync(new TimeoutException("db"));

        var e = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        Assert.AreEqual("boom", e.Message);
    }

    [TestMethod]
    public async Task CycleCommand_CompletedAndNoteCannotBeWritten_StillSucceeds()
    {
        SetupClaim(3, RecurringPaymentStatus.Completed);
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no way");
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>())).ReturnsAsync(refusal);
        _orderService.Setup(x => x.InsertOrderNote(It.IsAny<OrderNote>())).ThrowsAsync(new TimeoutException("db"));

        Assert.IsTrue(await CycleHandler().Handle(CycleCommand(), CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleCommand_CompletedAndProviderThrows_NotesItAndDoesNotRethrow()
    {
        SetupClaim(3, RecurringPaymentStatus.Completed);
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && n.Note ==
            "The recurring payment is completed, but the payment gateway did not confirm it stopped charging: network down")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_ClaimedWhileCancelled_AsksTheProviderToStopAndNotesTheRefusal()
    {
        SetupClaim(3, RecurringPaymentStatus.Cancelled);
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no way");
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>())).ReturnsAsync(refusal);

        Assert.IsTrue(await CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r => r.Id == "rp1")), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.Note == "The recurring payment is cancelled, but the payment gateway did not confirm it stopped charging: no way")),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_FinalOrderCannotBePlaced_StillStopsTheGatewayAndThrowsTheOriginalException()
    {
        SetupClaim(3, RecurringPaymentStatus.Completed);
        _mediator.Setup(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        var e = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        Assert.AreEqual("boom", e.Message);
        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r => r.Id == "rp1")), Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_AfterTheClaim_IgnoresTheRequestCancellation()
    {
        SetupClaim(2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await CycleHandler().Handle(CycleCommand(), cts.Token);

        _mediator.Verify(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_WithoutCycleReference_Throws()
    {
        var command = CycleCommand();
        command.CycleReference = "";

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            CycleHandler().Handle(command, CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleCommand_NegativeAmount_Throws()
    {
        var command = CycleCommand();
        command.Amount = -1;

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() =>
            CycleHandler().Handle(command, CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleCommand_NaNAmount_Throws()
    {
        var command = CycleCommand();
        command.Amount = double.NaN;

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() =>
            CycleHandler().Handle(command, CancellationToken.None));
    }

    [TestMethod]
    public async Task FailureCommand_WithoutCycleReference_Throws()
    {
        var handler = FailureHandler();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            handler.Handle(new RecordRecurringPaymentFailureCommand { RecurringPaymentId = "rp1", CycleReference = "" },
                CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleCommand_WithoutRecurringPaymentId_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            CycleHandler().Handle(new RecordRecurringPaymentCycleCommand { RecurringPaymentId = "" },
                CancellationToken.None));
    }
}
