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
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1
            },
            new() {
                ScheduleTaskName = "Delete guests",
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1440
            },
            new() {
                ScheduleTaskName = "Clear cache",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 120
            },
            new() {
                ScheduleTaskName = "Update currency exchange rates",
                Enabled = true,
                StopOnError = false,
                TimeInterval = 1440
            },
            new() {
                ScheduleTaskName = "Generate sitemap XML file",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 10080
            },
            new() {
                ScheduleTaskName = "End of the auctions",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 60
            },
            new() {
                ScheduleTaskName = "Cancel unpaid and pending orders",
                Enabled = false,
                StopOnError = false,
                TimeInterval = 1440
            }
        };
        tasks.ForEach(x => _scheduleTaskRepository.Insert(x));
        return Task.CompletedTask;
    }
}