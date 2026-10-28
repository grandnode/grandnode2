using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Infrastructure.Configuration;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class AuditLogRetentionScheduleTaskTests
{
    private Mock<IAuditService> _auditService;
    private DateTime _cutoff;

    [TestInitialize]
    public void Init()
    {
        _auditService = new Mock<IAuditService>();
        _auditService.Setup(s => s.DeleteOlderThan(It.IsAny<DateTime>()))
            .Callback<DateTime>(d => _cutoff = d)
            .ReturnsAsync(0);
    }

    [TestMethod]
    public async Task Execute_DeletesOlderThanRetention()
    {
        var task = new AuditLogRetentionScheduleTask(_auditService.Object, new AuditConfig { RetentionDays = 30 });

        await task.Execute(CancellationToken.None);

        var expected = DateTime.UtcNow.AddDays(-30);
        Assert.IsTrue(Math.Abs((_cutoff - expected).TotalSeconds) < 5, _cutoff.ToString("o"));
    }

    [TestMethod]
    public async Task Execute_Cancelled_DeletesNothing()
    {
        var task = new AuditLogRetentionScheduleTask(_auditService.Object, new AuditConfig());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => task.Execute(new CancellationToken(true)));

        _auditService.Verify(s => s.DeleteOlderThan(It.IsAny<DateTime>()), Times.Never);
    }

    [TestMethod]
    public async Task Execute_RetentionZero_KeepsEverything()
    {
        var task = new AuditLogRetentionScheduleTask(_auditService.Object, new AuditConfig { RetentionDays = 0 });

        await task.Execute(CancellationToken.None);

        _auditService.Verify(s => s.DeleteOlderThan(It.IsAny<DateTime>()), Times.Never);
    }
}
