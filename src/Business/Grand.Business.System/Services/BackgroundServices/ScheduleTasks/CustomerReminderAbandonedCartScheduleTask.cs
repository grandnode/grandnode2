﻿using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CustomerReminderAbandonedCartScheduleTask : IScheduleTask
    {
        private readonly ICustomerReminderService _customerReminderService;

        public CustomerReminderAbandonedCartScheduleTask(ICustomerReminderService customerReminderService)
        {
            _customerReminderService = customerReminderService;
        }

        public async Task Execute()
        {
            await _customerReminderService.Task_AbandonedCart();
        }
    }
}
