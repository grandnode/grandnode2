using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.System.Interfaces.ScheduleTasks;
using System.Threading.Tasks;

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
