using Grand.Business.Core.Interfaces.Authentication;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class DeleteOidcSessionsScheduleTaskTests
{
    [TestMethod]
    public async Task Execute_DeletesExpiredSessionsAndRevocations()
    {
        var sessions = new Mock<IOidcSessionService>();
        var revocations = new Mock<IOidcTokenRevocationService>();
        await new DeleteOidcSessionsScheduleTask(sessions.Object, revocations.Object).Execute(CancellationToken.None);
        sessions.Verify(x => x.DeleteExpired(), Times.Once);
        revocations.Verify(x => x.DeleteExpired(), Times.Once);
    }
}
