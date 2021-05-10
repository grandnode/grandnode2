using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
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
    public class CustomerReminderAbandonedCartScheduleTaskTests
    {
        private Mock<ICustomerReminderService> _crServiceMock;
        private CustomerReminderAbandonedCartScheduleTask _task;

        [TestInitialize]
        public void Init()
        {
            _crServiceMock = new Mock<ICustomerReminderService>();
            _task = new CustomerReminderAbandonedCartScheduleTask(_crServiceMock.Object);
        }

        [TestMethod]
        public async Task Execute_InovkeReminderService()
        {
            await _task.Execute();
            _crServiceMock.Verify(c => c.Task_AbandonedCart(It.IsAny<string>()), Times.Once);
        }
    }
}
