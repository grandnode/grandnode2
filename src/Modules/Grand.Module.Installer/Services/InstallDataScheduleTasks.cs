using Grand.Domain.Tasks;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallScheduleTasks()
    {
        //these tasks are default - they are created in order to insert them into database
        //and nothing above it
        //there is no need to send arguments into ctor - all are null
        var tasks = new List<ScheduleTask> {
            new() {
                ScheduleTaskName = "Send emails",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.QueuedMessagesSendScheduleTask, Grand.Business.System",
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1
            },
            new() {
                ScheduleTaskName = "Delete guests",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.DeleteGuestsScheduleTask, Grand.Business.System",
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1440
            },
            new() {
                ScheduleTaskName = "Clear cache",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.ClearCacheScheduleTask, Grand.Business.System",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 120
            },
            new() {
                ScheduleTaskName = "Update currency exchange rates",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.UpdateExchangeRateScheduleTask, Grand.Business.System",
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1440
            },
            new() {
                ScheduleTaskName = "Generate sitemap XML file",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.GenerateSitemapXmlTask, Grand.Business.System",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 10080
            },
            new() {
                ScheduleTaskName = "End of the auctions",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.EndAuctionsTask, Grand.Business.System",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 60
            },
            new() {
                ScheduleTaskName = "Cancel unpaid and pending orders",
                Type =
                    "Grand.Module.Installer.BackgroundServices.ScheduleTasks.CancelOrderScheduledTask, Grand.Business.System",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 1440
            }
        };
        tasks.ForEach(x => _scheduleTaskRepository.Insert(x));
        return Task.CompletedTask;
    }
}