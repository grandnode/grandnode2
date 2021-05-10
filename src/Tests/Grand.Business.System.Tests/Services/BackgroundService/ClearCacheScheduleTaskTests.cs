using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Services.BackgroundService
{
    [TestClass]
    public class ClearCacheScheduleTaskTests
    {
        private Mock<ICacheBase> _cacheMock;
        private ClearCacheScheduleTask _task;

        [TestInitialize]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _task = new ClearCacheScheduleTask(_cacheMock.Object);
        }

        [TestMethod]
        public async Task Execute_InvokeExpectedMethod()
        {
            await _task.Execute();
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }
    }
}
