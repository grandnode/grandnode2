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

    [TestMethod]
    public async Task FailureCommand_PassesTheReportToTheService()
    {
        _service.Setup(x => x.RecordFailure("rp1", "in_2", "declined")).ReturnsAsync(true);
        var handler = new RecordRecurringPaymentFailureCommandHandler(_service.Object);

        var result = await handler.Handle(new RecordRecurringPaymentFailureCommand {
            RecurringPaymentId = "rp1", CycleReference = "in_2", Reason = "declined"
        }, CancellationToken.None);

        Assert.IsTrue(result);
        _service.Verify(x => x.RecordFailure("rp1", "in_2", "declined"), Times.Once);
    }

    [TestMethod]
    public async Task FailureCommand_WithoutRecurringPaymentId_Throws()
    {
        var handler = new RecordRecurringPaymentFailureCommandHandler(_service.Object);

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

    private void SetupClaim(int cycleNumber, bool completed)
    {
        var claim = new RecurringCycleClaim {
            RecurringPayment = new RecurringPayment {
                Id = "rp1", InitialOrderId = "o1", PaymentMethodSystemName = "Payments.Test"
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
        SetupClaim(1, false);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _mediator.Verify(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_LaterCycle_PlacesTheCycleOrder()
    {
        SetupClaim(2, false);

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _mediator.Verify(x => x.Send(It.Is<PlaceRecurringCycleOrderCommand>(c =>
            c.RecurringPayment.Id == "rp1" && c.Cycle.CycleNumber == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_OrderCannotBeCreated_NotesItOnTheInitialOrderAndThrows()
    {
        SetupClaim(2, false);
        _mediator.Setup(x => x.Send(It.IsAny<PlaceRecurringCycleOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CycleHandler().Handle(CycleCommand(), CancellationToken.None));

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && n.Note.Contains("in_2") && n.Note.Contains("boom") && !n.DisplayToCustomer)),
            Times.Once);
    }

    [TestMethod]
    public async Task CycleCommand_Completed_AsksTheProviderToStop()
    {
        SetupClaim(3, true);
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        var result = await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        Assert.IsTrue(result);
        _paymentService.Verify(x => x.CancelRecurringPayment(It.Is<RecurringPayment>(r => r.Id == "rp1")), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_NotCompleted_DoesNotAskTheProviderToStop()
    {
        SetupClaim(3, false);

        await CycleHandler().Handle(CycleCommand(), CancellationToken.None);

        _paymentService.Verify(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [TestMethod]
    public async Task CycleCommand_CompletedAndProviderRefuses_NotesItOnTheInitialOrder()
    {
        SetupClaim(3, true);
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
        SetupClaim(2, false);
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
        SetupClaim(3, true);
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no way");
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>())).ReturnsAsync(refusal);
        _orderService.Setup(x => x.InsertOrderNote(It.IsAny<OrderNote>())).ThrowsAsync(new TimeoutException("db"));

        Assert.IsTrue(await CycleHandler().Handle(CycleCommand(), CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleCommand_CompletedAndProviderThrows_NotesItAndDoesNotRethrow()
    {
        SetupClaim(3, true);
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
    public async Task CycleCommand_WithoutRecurringPaymentId_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            CycleHandler().Handle(new RecordRecurringPaymentCycleCommand { RecurringPaymentId = "" },
                CancellationToken.None));
    }
}
