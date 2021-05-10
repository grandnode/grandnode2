using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Directory;
using Grand.Domain.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallScheduleTasks()
        {
            //these tasks are default - they are created in order to insert them into database
            //and nothing above it
            //there is no need to send arguments into ctor - all are null
            var tasks = new List<ScheduleTask>
            {
            new ScheduleTask
            {
                    ScheduleTaskName = "Send emails",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.QueuedMessagesSendScheduleTask, Grand.Business.System",
                    Enabled = true,
                    StopOnError = false,
                    TimeInterval = 1
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Delete guests",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.DeleteGuestsScheduleTask, Grand.Business.System",
                    Enabled = true,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Clear cache",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.ClearCacheScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 120
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Clear log",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.ClearLogScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Update currency exchange rates",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.UpdateExchangeRateScheduleTask, Grand.Business.System",
                    Enabled = true,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Generate sitemap XML file",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.GenerateSitemapXmlTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 10080
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - AbandonedCart",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderAbandonedCartScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 20
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - RegisteredCustomer",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderRegisteredCustomerScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - LastActivity",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderLastActivityScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - LastPurchase",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderLastPurchaseScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - Birthday",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderBirthdayScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - Completed order",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderCompletedOrderScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Customer reminder - Unpaid order",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CustomerReminderUnpaidOrderScheduleTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "End of the auctions",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.EndAuctionsTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 60
                },
                new ScheduleTask
                {
                    ScheduleTaskName = "Cancel unpaid and pending orders",
                    Type = "Grand.Business.System.Services.BackgroundServices.ScheduleTasks.CancelOrderScheduledTask, Grand.Business.System",
                    Enabled = false,
                    StopOnError = false,
                    TimeInterval = 1440
                },
            };
            await _scheduleTaskRepository.InsertAsync(tasks);
        }
    }
}
