using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Domain.Common;
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
    public class CancelOrderScheduledTaskTests
    {
        private Mock<IOrderService> _orderServiceMock;
        private SystemSettings _settings;
        private CancelOrderScheduledTask _task;

        [TestInitialize]
        public void Init()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _settings = new SystemSettings();
            _task = new CancelOrderScheduledTask(_settings,_orderServiceMock.Object);
        }

        [TestMethod]
        public async Task Execute_InvokeExpectedMethods()
        {
            _settings.DaysToCancelUnpaidOrder = 1;
            await _task.Execute();
            _orderServiceMock.Verify(c => c.CancelExpiredOrders(It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_SettingsNotHaveValue_NotInvokeAnyMethods()
        {
            _settings.DaysToCancelUnpaidOrder = null;
            await _task.Execute();
            _orderServiceMock.Verify(c => c.CancelExpiredOrders(It.IsAny<DateTime>()), Times.Never);
        }
    }
}
