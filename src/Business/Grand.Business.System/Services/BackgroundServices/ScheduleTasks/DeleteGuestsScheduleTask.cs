﻿using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Common;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    /// <summary>
    /// Represents a task for deleting guest customers
    /// </summary>
    public class DeleteGuestsScheduleTask : IScheduleTask
    {
        private readonly ICustomerService _customerService;
        private readonly SystemSettings _systemSettings;

        public DeleteGuestsScheduleTask(ICustomerService customerService, SystemSettings systemSettings)
        {
            _customerService = customerService;
            _systemSettings = systemSettings;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public async Task Execute()
        {
            var olderThanMinutes = _systemSettings.DeleteGuestTaskOlderThanMinutes;
            // Default value in case 0 is returned.  0 would effectively disable this service and harm performance.
            olderThanMinutes = olderThanMinutes == 0 ? 1440 : olderThanMinutes;
            await _customerService.DeleteGuestCustomers(null, DateTime.UtcNow.AddMinutes(-olderThanMinutes), true);
        }
    }
}
