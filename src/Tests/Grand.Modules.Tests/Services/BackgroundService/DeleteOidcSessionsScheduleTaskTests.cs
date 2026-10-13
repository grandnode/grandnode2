using Grand.Business.Core.Interfaces.Authentication;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class DeleteOidcSessionsScheduleTaskTests
{
    [TestMethod]
    public async Task Execute_DeletesExpiredSessions()
    {
        var sessions = new Mock<IOidcSessionService>();
        await new DeleteOidcSessionsScheduleTask(sessions.Object).Execute();
        sessions.Verify(x => x.DeleteExpired(), Times.Once);
    }
}
