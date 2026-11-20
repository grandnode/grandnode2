using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class RecordRecurringPaymentCommandHandlersTests
{
    private Mock<IRecurringPaymentService> _service;

    [TestInitialize]
    public void Init()
    {
        _service = new Mock<IRecurringPaymentService>();
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
}
