using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class PaymentDueReminderScheduleTaskTests
{
    [TestMethod]
    public async Task Execute_SendsRemindersAsOfNowInUtc()
    {
        var serviceMock = new Mock<IPaymentReminderService>();
        using var cts = new CancellationTokenSource();
        var before = DateTime.UtcNow;

        await new PaymentDueReminderScheduleTask(serviceMock.Object).Execute(cts.Token);

        serviceMock.Verify(x => x.SendPaymentReminders(
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc && d >= before && d <= DateTime.UtcNow), cts.Token));
    }
}
