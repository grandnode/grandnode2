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
    public class CustomerReminderTasksTests
    {
        private Mock<ICustomerReminderService> _customerRemiderServiceMock;
        private CustomerReminderBirthdayScheduleTask _customerReminderBirthdayTask;
        private CustomerReminderCompletedOrderScheduleTask _completedOrderTask;
        private CustomerReminderLastActivityScheduleTask _lastActivityScheduleTask;
        private CustomerReminderLastPurchaseScheduleTask _LastPurchaseScheduleTask;
        private CustomerReminderRegisteredCustomerScheduleTask _registeredCustomerScheduleTask;
        private CustomerReminderUnpaidOrderScheduleTask _unpaidOrderScheduleTask;

        [TestInitialize]
        public void Init()
        {
            _customerRemiderServiceMock = new Mock<ICustomerReminderService>();
            _customerReminderBirthdayTask = new CustomerReminderBirthdayScheduleTask(_customerRemiderServiceMock.Object);
            _completedOrderTask = new CustomerReminderCompletedOrderScheduleTask(_customerRemiderServiceMock.Object);
            _lastActivityScheduleTask = new CustomerReminderLastActivityScheduleTask(_customerRemiderServiceMock.Object);
            _LastPurchaseScheduleTask = new CustomerReminderLastPurchaseScheduleTask(_customerRemiderServiceMock.Object);
            _registeredCustomerScheduleTask = new CustomerReminderRegisteredCustomerScheduleTask(_customerRemiderServiceMock.Object);
            _unpaidOrderScheduleTask = new CustomerReminderUnpaidOrderScheduleTask(_customerRemiderServiceMock.Object);
        }

        [TestMethod]
        public async Task BirthdayTaskExecute_InvokeExpectedMethod()
        {
            await _customerReminderBirthdayTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_Birthday(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task CompletedOrderTaskExecute_InvokeExpectedMethod()
        {
            await _completedOrderTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_CompletedOrder(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task LastActivityScheduleTaskExecute_InvokeExpectedMethod()
        {
            await _lastActivityScheduleTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_LastActivity(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task LastPurchaseScheduleTaskExecute_InvokeExpectedMethod()
        {
            await _LastPurchaseScheduleTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_LastPurchase(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task RegisteredCustomerScheduleTaskExecute_InvokeExpectedMethod()
        {
            await _registeredCustomerScheduleTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_RegisteredCustomer(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task UnpaidOrderScheduleTaskExecute_InvokeExpectedMethod()
        {
            await _unpaidOrderScheduleTask.Execute();
            _customerRemiderServiceMock.Verify(c => c.Task_UnpaidOrder(It.IsAny<string>()), Times.Once);
        }
    }
}
