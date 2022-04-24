using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CustomerReminderLastPurchaseScheduleTask : IScheduleTask
    {
        private readonly ICustomerReminderService _customerReminderService;
        public CustomerReminderLastPurchaseScheduleTask(ICustomerReminderService customerReminderService)
        {
            _customerReminderService = customerReminderService;
        }

        public async Task Execute()
        {
            await _customerReminderService.Task_LastPurchase();
        }
    }
}
