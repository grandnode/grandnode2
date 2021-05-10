using Grand.Business.Customers.Interfaces;
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
    public class DeleteGuestsScheduleTaskTests
    {
        private Mock<ICustomerService> _customerServiceMock;
        private SystemSettings _settings;
        private DeleteGuestsScheduleTask _task;

        [TestInitialize]
        public void Init()
        {
            _customerServiceMock = new Mock<ICustomerService>();
            _settings = new SystemSettings();
            _task = new DeleteGuestsScheduleTask(_customerServiceMock.Object,_settings);
        }

        [TestMethod]
        public async Task Execute_InovkeExpectedMethod()
        {
            _settings.DeleteGuestTaskOlderThanMinutes = 0;
            await _task.Execute();
            _customerServiceMock.Verify(c => c.DeleteGuestCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool>()), Times.Once);

        }
    }
}
