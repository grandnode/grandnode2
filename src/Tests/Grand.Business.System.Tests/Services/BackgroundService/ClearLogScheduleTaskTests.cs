using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Services.BackgroundService
{
    [TestClass]
    public class ClearLogScheduleTaskTests
    {
        private Mock<ILogger> _loggerMock;
        private ClearLogScheduleTask _task;

        [TestInitialize]
        public void Init()
        {
            _loggerMock = new Mock<ILogger>();
            _task = new ClearLogScheduleTask(_loggerMock.Object);
        }

        [TestMethod]
        public async Task Execute_InvokeClearLogs()
        {
            await _task.Execute();
            _loggerMock.Verify(c => c.ClearLog(), Times.Once);
        }
    }
}
