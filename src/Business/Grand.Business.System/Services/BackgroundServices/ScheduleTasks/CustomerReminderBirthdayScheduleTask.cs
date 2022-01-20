﻿using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.System.Interfaces.ScheduleTasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CustomerReminderBirthdayScheduleTask : IScheduleTask
    {
        private readonly ICustomerReminderService _customerReminderService;

        public CustomerReminderBirthdayScheduleTask(ICustomerReminderService customerReminderService)
        {
            _customerReminderService = customerReminderService;
        }

        public async Task Execute()
        {
            await _customerReminderService.Task_Birthday();
        }
    }
}
