using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CustomerReminderUnpaidOrderScheduleTask : IScheduleTask
    {
        private readonly ICustomerReminderService _customerReminderService;
        public CustomerReminderUnpaidOrderScheduleTask(ICustomerReminderService customerReminderService)
        {
            _customerReminderService = customerReminderService;
        }

        public async Task Execute()
        {
            await _customerReminderService.Task_UnpaidOrder();
        }
    }
}
