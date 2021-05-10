using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.System.Interfaces.ScheduleTasks;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CustomerReminderRegisteredCustomerScheduleTask : IScheduleTask
    {
        private readonly ICustomerReminderService _customerReminderService;
        public CustomerReminderRegisteredCustomerScheduleTask(ICustomerReminderService customerReminderService)
        {
            _customerReminderService = customerReminderService;
        }

        public async Task Execute()
        {
            await _customerReminderService.Task_RegisteredCustomer();
        }
    }
}
