﻿using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
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
